import { describe, expect, it } from 'vitest'
import type { SgNode } from '../app/types/roku-dev-tools'
import {
  containRect,
  findScene,
  isHidden,
  nodeLabel,
  nodeSignature,
  parseBounds,
  pathForKey,
  sceneBounds,
  toHighlight,
  uiSize,
} from '../app/lib/scenegraph/bounds'

function node(type: string, attributes: Record<string, string> = {}, children: SgNode[] = []): SgNode {
  return { type, attributes, children }
}

describe('parseBounds', () => {
  it('reads the Roku format', () => {
    expect(parseBounds('{0, 0, 1920, 1080}')).toEqual({ x: 0, y: 0, width: 1920, height: 1080 })
  })

  it('accepts negatives and decimals', () => {
    expect(parseBounds('{-12.5, 40, 300.25, 0}')).toEqual({ x: -12.5, y: 40, width: 300.25, height: 0 })
  })

  it('rejects missing or malformed values', () => {
    expect(parseBounds(undefined)).toBeNull()
    expect(parseBounds('')).toBeNull()
    expect(parseBounds('{1, 2, 3}')).toBeNull()
    expect(parseBounds('{a, b, c, d}')).toBeNull()
  })
})

describe('uiSize', () => {
  it('uses the Scene node, matched by type or extends', () => {
    expect(uiSize([node('MainScene', { extends: 'Scene', bounds: '{0, 0, 1280, 720}' })])).toEqual({
      width: 1280,
      height: 720,
    })
    expect(uiSize([node('Scene', { bounds: '{0, 0, 1920, 1080}' })])).toEqual({ width: 1920, height: 1080 })
  })

  it('snaps odd sizes to the nearest UI resolution', () => {
    expect(uiSize([node('Scene', { bounds: '{0, 0, 2400, 1100}' })])).toEqual({ width: 1920, height: 1080 })
    expect(uiSize([node('Scene', { bounds: '{0, 0, 1300, 760}' })])).toEqual({ width: 1280, height: 720 })
  })

  it('is null without a usable Scene', () => {
    expect(uiSize([node('Group', { bounds: '{0, 0, 1920, 1080}' })])).toBeNull()
    expect(uiSize([node('Scene', { bounds: '{0, 0, 0, 0}' })])).toBeNull()
    expect(findScene([])).toBeNull()
  })
})

describe('containRect', () => {
  it('fills a box of the same aspect', () => {
    expect(containRect(1600, 900, 1920, 1080)).toEqual({ x: 0, y: 0, width: 1600, height: 900 })
  })

  it('pillarboxes a wide box', () => {
    expect(containRect(2560, 1080, 1920, 1080)).toEqual({ x: 320, y: 0, width: 1920, height: 1080 })
  })

  it('letterboxes a tall box', () => {
    expect(containRect(1920, 1200, 1920, 1080)).toEqual({ x: 0, y: 60, width: 1920, height: 1080 })
  })

  it('falls back to the box when a size is unknown', () => {
    expect(containRect(800, 450, 0, 0)).toEqual({ x: 0, y: 0, width: 800, height: 450 })
  })
})

describe('pathForKey', () => {
  const leaf = node('Label', { name: 'title' })
  const group = node('Group', {}, [node('Poster'), leaf])
  const tree = [node('MainScene', {}, [group])]

  it('walks the index path from the root', () => {
    expect(pathForKey(tree, '0-0-1')).toEqual([tree[0], group, leaf])
  })

  it('is null for keys that do not resolve', () => {
    expect(pathForKey(tree, '0-5')).toBeNull()
    expect(pathForKey(tree, '0-x')).toBeNull()
    expect(pathForKey([], '0')).toBeNull()
  })
})

describe('isHidden', () => {
  it('follows visibility and opacity down the path', () => {
    expect(isHidden([node('Scene'), node('Poster', { visible: 'true', opacity: '1' })])).toBe(false)
    expect(isHidden([node('Group', { visible: 'false' }), node('Poster')])).toBe(true)
    expect(isHidden([node('Group', { opacity: '0.0' }), node('Poster')])).toBe(true)
    expect(isHidden([node('Poster', { opacity: '' })])).toBe(false)
  })
})

describe('nodeLabel', () => {
  it('adds the id when there is one', () => {
    expect(nodeLabel(node('Label', { name: 'title' }))).toBe('Label #title')
    expect(nodeLabel(node('Poster'))).toBe('Poster')
  })
})

describe('sceneBounds', () => {
  const scene = node('MainScene', { extends: 'Scene', bounds: '{0, 0, 1920, 1080}' })

  function expectRect(actual: ReturnType<typeof sceneBounds>, x: number, y: number, width: number, height: number) {
    expect(actual).not.toBeNull()
    expect(actual!.x).toBeCloseTo(x)
    expect(actual!.y).toBeCloseTo(y)
    expect(actual!.width).toBeCloseTo(width)
    expect(actual!.height).toBeCloseTo(height)
  }

  it('uses top-level bounds as they are, since they already include the node transform', () => {
    // From a real dump: a 1920×1080 Poster, translated and scaled about a center.
    const poster = node('Poster', {
      translation: '{0, -128.92401}',
      scale: '{1.0472159, 1.0472159}',
      scaleRotateCenter: '{936, 453.924}',
      bounds: '{-44.19409, -150.35645, 2010.6545, 1130.9932}',
    })
    expectRect(sceneBounds([scene, poster]), -44.19409, -150.35645, 2010.6545, 1130.9932)
  })

  it('adds the translations of ancestors', () => {
    const heroText = node('RenderableNode', { translation: '{142, 0}' })
    const eyebrow = node('Label', { translation: '{0, 211}', bounds: '{0, 211, 171, 21}' })
    expectRect(sceneBounds([scene, heroText, eyebrow]), 142, 211, 171, 21)
  })

  it('follows a scrolled container', () => {
    const content = node('RenderableNode', { translation: '{0, -700}' })
    const shelves = node('RowList', { translation: '{142, 938}', bounds: '{120, 916, 1822, 1286}' })
    const row = node('MarkupGrid', { translation: '{0, 50}', bounds: '{-22, 28, 1822, 362}' })
    expectRect(sceneBounds([scene, content, shelves, row]), 120, 266, 1822, 362)
  })

  it('applies ancestor scale about its center', () => {
    const button = node('PillButton', { translation: '{0, 536}' })
    const body = node('RenderableNode', { scale: '{1.05, 1.05}', scaleRotateCenter: '{95.5, 29}' })
    const label = node('Label', { translation: '{60, 0}', bounds: '{60, 0, 107, 58}' })
    expectRect(sceneBounds([scene, button, body, label]), 58.225, 534.55, 112.35, 60.9)
  })

  it('boxes an ancestor rotation (counterclockwise)', () => {
    const turned = node('Group', { rotation: '1.5707963', translation: '{100, 500}' })
    const child = node('Rectangle', { bounds: '{0, 0, 200, 50}' })
    // A quarter turn counterclockwise sends +x up the screen.
    expectRect(sceneBounds([scene, turned, child]), 100, 300, 50, 200)
  })

  it('stops at a node that does not inherit its parent transform', () => {
    const moved = node('Group', { translation: '{500, 500}' })
    const overlay = node('Group', { inheritParentTransform: 'false', translation: '{10, 20}' })
    const child = node('Rectangle', { bounds: '{5, 5, 30, 30}' })
    expectRect(sceneBounds([scene, moved, overlay, child]), 15, 25, 30, 30)
    expectRect(
      sceneBounds([scene, moved, node('Rectangle', { inheritParentTransform: 'false', bounds: '{5, 5, 30, 30}' })]),
      5,
      5,
      30,
      30,
    )
  })

  it('is null without bounds', () => {
    expect(sceneBounds([scene, node('Timer')])).toBeNull()
    expect(sceneBounds([])).toBeNull()
  })
})

describe('toHighlight', () => {
  const ui = { width: 1280, height: 720 }

  it('places nested nodes in Scene coordinates', () => {
    const path = [node('Group', { translation: '{40, 60}' }), node('Poster', { bounds: '{10, 20, 300, 200}' })]
    expect(toHighlight(path, ui)!.bounds).toEqual({ x: 50, y: 80, width: 300, height: 200 })
  })

  it('describes the last node of the path', () => {
    const path = [node('Group', { visible: 'false' }), node('Poster', { name: 'hero', bounds: '{10, 20, 300, 200}' })]
    expect(toHighlight(path, ui)).toEqual({
      label: 'Poster #hero',
      bounds: { x: 10, y: 20, width: 300, height: 200 },
      uiSize: ui,
      hidden: true,
    })
  })

  it('is null without a node or bounds', () => {
    expect(toHighlight(null, ui)).toBeNull()
    expect(toHighlight([node('Poster')], ui)).toBeNull()
  })

  it('can be posted to another window', () => {
    const h = toHighlight([node('Poster', { bounds: '{0, 0, 1, 1}' })], ui)
    expect(structuredClone(h)).toEqual(h)
    expect(h!.uiSize).not.toBe(ui)
  })
})

describe('nodeSignature', () => {
  it('combines type and id', () => {
    expect(nodeSignature(node('Label', { name: 'title' }))).toBe('Label#title')
    expect(nodeSignature(node('Poster'))).toBe('Poster#')
  })
})
