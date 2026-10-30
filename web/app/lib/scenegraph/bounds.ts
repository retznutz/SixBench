import type { SgHighlight, SgNode, SgRect, SgSize } from '../../types/roku-dev-tools'

/** Roku UI resolutions a channel can declare (ui_resolutions in its manifest): FHD, HD, SD. */
export const UI_RESOLUTIONS: readonly SgSize[] = [
  { width: 1920, height: 1080 },
  { width: 1280, height: 720 },
  { width: 720, height: 480 },
]

/** Used until a capture includes the Scene node. */
export const DEFAULT_UI_SIZE: SgSize = UI_RESOLUTIONS[0]!

const NUMBER = /-?\d+(?:\.\d+)?(?:e[-+]?\d+)?/gi

/** Parses a field of `length` numbers (`"{x, y}"`, `"1.5"`); null when missing or malformed. */
function parseNumbers(value: string | undefined, length: number): number[] | null {
  const n = value?.match(NUMBER)?.map(Number)
  return n && n.length === length && n.every(Number.isFinite) ? n : null
}

/**
 * Parses a node's `bounds` field (`"{x, y, w, h}"`); null when missing or malformed. The Roku
 * reports it in the parent's coordinates: the node's own translation, scale and rotation are applied.
 */
export function parseBounds(value: string | undefined): SgRect | null {
  const n = parseNumbers(value, 4)
  if (!n) return null
  const [x, y, width, height] = n as [number, number, number, number]
  return { x, y, width, height }
}

interface Point {
  x: number
  y: number
}

/**
 * Maps a point from a node's coordinates to its parent's: scale and rotate about
 * `scaleRotateCenter`, then translate. Fields the dump leaves out are at their defaults.
 */
function toParent(node: SgNode, p: Point): Point {
  const a = node.attributes
  const [tx, ty] = parseNumbers(a.translation, 2) ?? [0, 0]
  const [sx, sy] = parseNumbers(a.scale, 2) ?? [1, 1]
  const [cx, cy] = parseNumbers(a.scaleRotateCenter, 2) ?? [0, 0]
  const [r] = parseNumbers(a.rotation, 1) ?? [0]
  const x = (p.x - cx!) * sx!
  const y = (p.y - cy!) * sy!
  // Positive rotation is counterclockwise on screen, where y points down.
  const cos = Math.cos(r!)
  const sin = Math.sin(r!)
  return { x: cx! + x * cos + y * sin + tx!, y: cy! - x * sin + y * cos + ty! }
}

/**
 * Where the node at the end of `path` is drawn, in the Scene's coordinates: its `bounds` mapped
 * through every ancestor's transform (so nested and scrolled groups land in the right place), as
 * the axis-aligned box around the result. A node with `inheritParentTransform` off is placed
 * relative to the Scene, so composing stops there. Null when the node has no bounds.
 */
export function sceneBounds(path: SgNode[]): SgRect | null {
  const bounds = parseBounds(path.at(-1)?.attributes.bounds)
  if (!bounds) return null
  const detached = path.findLastIndex((n) => n.attributes.inheritParentTransform === 'false')
  const ancestors = path.slice(Math.max(detached, 0), -1).reverse()
  const { x, y, width, height } = bounds
  let corners: Point[] = [
    { x, y },
    { x: x + width, y },
    { x, y: y + height },
    { x: x + width, y: y + height },
  ]
  for (const ancestor of ancestors) corners = corners.map((p) => toParent(ancestor, p))
  const xs = corners.map((p) => p.x)
  const ys = corners.map((p) => p.y)
  const left = Math.min(...xs)
  const top = Math.min(...ys)
  return { x: left, y: top, width: Math.max(...xs) - left, height: Math.max(...ys) - top }
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

/** A node's tree label: its type, plus its id when it has one. */
export function nodeLabel(node: SgNode): string {
  const name = node.attributes.name
  return name ? `${node.type} #${name}` : node.type
}

/** What to outline for the node at the end of `path`; null when there is no node or it has no bounds. */
export function toHighlight(path: SgNode[] | null, uiSize: SgSize): SgHighlight | null {
  const node = path?.at(-1)
  const bounds = path ? sceneBounds(path) : null
  if (!path || !node || !bounds) return null
  // Copy the size: it may be a reactive proxy, which can't be posted to another window.
  return {
    label: nodeLabel(node),
    bounds,
    uiSize: { width: uiSize.width, height: uiSize.height },
    hidden: isHidden(path),
  }
}

/** Identifies a node across captures well enough to keep it selected on refresh. */
export function nodeSignature(node: SgNode): string {
  return `${node.type}#${node.attributes.name ?? ''}`
}
