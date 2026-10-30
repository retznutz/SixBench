/** Which SceneGraph nodes to dump; must match SixBench.Common.Enums.SgNodeScope. */
export type SgNodeScope = 'All' | 'Roots' | 'Nodes'

/** One SceneGraph node (GET /roku-devices/{id}/dev-tools/sgnodes). */
export interface SgNode {
  /** Node type, e.g. `Poster` or a component name. */
  type: string
  /** Every field the Roku reported, as strings. */
  attributes: Record<string, string>
  children: SgNode[]
}

/** A rectangle in SceneGraph UI coordinates (pixels of the channel's design resolution). */
export interface SgRect {
  x: number
  y: number
  width: number
  height: number
}

/** Size of a channel's UI coordinate space, e.g. 1920×1080 for an FHD channel. */
export interface SgSize {
  width: number
  height: number
}

/**
 * A node to outline on the video. Plain data, so it can be sent to the watch page from a
 * popped-out developer tools window.
 */
export interface SgHighlight {
  /** e.g. `Poster #hero`. */
  label: string
  bounds: SgRect
  /** The UI coordinate space `bounds` is in. */
  uiSize: SgSize
  /** The node or an ancestor is invisible or fully transparent. */
  hidden: boolean
}

/** Messages on the per-Roku channel between a popped-out developer tools window and watch pages. */
export type SgHighlightMessage =
  | { kind: 'highlight'; highlight: SgHighlight | null }
  /** Sent by a watch page when it starts listening, so an open pop-out answers with its current highlight. */
  | { kind: 'request' }

/** Response of GET /roku-devices/{id}/dev-tools/sgnodes. */
export interface SgNodesResult {
  scope: SgNodeScope
  nodes: SgNode[]
  totalNodes: number
  retrievedUtc: string
}

/** Query for GET /roku-devices/{id}/dev-tools/sgnodes. */
export interface SgNodesQuery {
  scope: SgNodeScope
  nodeId?: string
  sizes?: boolean
}

/** Response of GET /roku-devices/{id}/dev-tools/chanperf. */
export interface ChanPerf {
  appId: string | null
  timestampMs: number | null
  cpuDurationSeconds: number | null
  cpuUserPercent: number | null
  cpuSysPercent: number | null
  memoryUsedBytes: number | null
  memoryResidentBytes: number | null
  memoryAnonBytes: number | null
  memoryFileBytes: number | null
  memorySharedBytes: number | null
  memorySwapBytes: number | null
  processId: number | null
}

/** A chanperf reading stamped with the browser's receive time. */
export interface ChanPerfSample {
  /** Date.now() when the sample arrived. */
  at: number
  perf: ChanPerf
}

/** One registry entry. */
export interface RokuRegistryItem {
  key: string
  value: string
}

/** One registry section. */
export interface RokuRegistrySection {
  name: string
  items: RokuRegistryItem[]
}

/** Response of GET /roku-devices/{id}/dev-tools/registry/{appId}. */
export interface RokuRegistry {
  appId: string
  devId: string | null
  spaceAvailableBytes: number | null
  sections: RokuRegistrySection[]
}
