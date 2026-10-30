import type { SgNode, SgRect, SgSize } from '../../types/roku-dev-tools'

/** Roku UI resolutions a channel can declare (ui_resolutions in its manifest): FHD, HD, SD. */
export const UI_RESOLUTIONS: readonly SgSize[] = [
  { width: 1920, height: 1080 },
  { width: 1280, height: 720 },
  { width: 720, height: 480 },
]

/** Used until a capture includes the Scene node. */
export const DEFAULT_UI_SIZE: SgSize = UI_RESOLUTIONS[0]!

const NUMBER = /-?\d+(?:\.\d+)?(?:e[-+]?\d+)?/gi

/** Parses a node's `bounds` field (`"{x, y, w, h}"`); null when missing or malformed. */
export function parseBounds(value: string | undefined): SgRect | null {
  const n = value?.match(NUMBER)?.map(Number)
  if (!n || n.length !== 4 || n.some((v) => !Number.isFinite(v))) return null
  const [x, y, width, height] = n as [number, number, number, number]
  return { x, y, width, height }
}

/** The Scene root of a capture: the node whose size is the channel's UI coordinate space. */
export function findScene(nodes: SgNode[]): SgNode | null {
  return nodes.find((n) => n.type === 'Scene' || n.attributes.extends === 'Scene') ?? null
}

/**
 * The channel's UI coordinate space, from the Scene node's bounds, snapped to the nearest Roku UI
 * resolution so content spilling past the screen edge doesn't skew it. Null when there is no Scene
 * (e.g. a "by id" capture).
 */
export function uiSize(nodes: SgNode[]): SgSize | null {
  const bounds = parseBounds(findScene(nodes)?.attributes.bounds)
  if (!bounds || bounds.width <= 0 || bounds.height <= 0) return null
  return UI_RESOLUTIONS.reduce((best, r) =>
    Math.abs(r.height - bounds.height) < Math.abs(best.height - bounds.height) ? r : best,
  )
}

/**
 * Where a `srcWidth`×`srcHeight` picture lands inside a `boxWidth`×`boxHeight` element with
 * `object-fit: contain` (centered, letterboxed). Falls back to the whole box when a size is unknown.
 */
export function containRect(boxWidth: number, boxHeight: number, srcWidth: number, srcHeight: number): SgRect {
  if (boxWidth <= 0 || boxHeight <= 0 || srcWidth <= 0 || srcHeight <= 0) {
    return { x: 0, y: 0, width: Math.max(boxWidth, 0), height: Math.max(boxHeight, 0) }
  }
  const scale = Math.min(boxWidth / srcWidth, boxHeight / srcHeight)
  const width = srcWidth * scale
  const height = srcHeight * scale
  return { x: (boxWidth - width) / 2, y: (boxHeight - height) / 2, width, height }
}

/** The nodes from the root down to the one at an index-path key ("0-3-1"); null if the key doesn't resolve. */
export function pathForKey(nodes: SgNode[], key: string): SgNode[] | null {
  const path: SgNode[] = []
  let level = nodes
  for (const part of key.split('-')) {
    const node = /^\d+$/.test(part) ? level[Number(part)] : undefined
    if (!node) return null
    path.push(node)
    level = node.children
  }
  return path.length ? path : null
}

/** True when the node or any ancestor is invisible or fully transparent, so it isn't drawn. */
export function isHidden(path: SgNode[]): boolean {
  return path.some((n) => n.attributes.visible === 'false' || Number.parseFloat(n.attributes.opacity ?? '') === 0)
}

/** Identifies a node across captures well enough to keep it selected on refresh. */
export function nodeSignature(node: SgNode): string {
  return `${node.type}#${node.attributes.name ?? ''}`
}
