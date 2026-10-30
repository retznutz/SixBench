import { describe, expect, it } from 'vitest'
import type { SgNode } from '../app/types/roku-dev-tools'
import {
  containRect,
  findScene,
  isHidden,
  nodeSignature,
  parseBounds,
  pathForKey,
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

describe('nodeSignature', () => {
  it('combines type and id', () => {
    expect(nodeSignature(node('Label', { name: 'title' }))).toBe('Label#title')
    expect(nodeSignature(node('Poster'))).toBe('Poster#')
  })
})
