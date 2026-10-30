import { defineStore } from 'pinia'
import type { ApiError } from '~/types/api-error'
import type {
  ChanPerf,
  ChanPerfSample,
  RokuRegistry,
  SgHighlight,
  SgNode,
  SgNodesQuery,
  SgNodesResult,
  SgSize,
} from '~/types/roku-dev-tools'
import { DEFAULT_UI_SIZE, nodeSignature, pathForKey, toHighlight, uiSize } from '~/lib/scenegraph/bounds'

/** How many performance samples to keep for the trend lines. */
export const PERF_HISTORY = 60

/**
 * Roku developer tools: SceneGraph dumps, channel performance polling and registry reads.
 * Requests are silent; errors are kept in state and shown inline because "developer mode is off"
 * is an expected answer, not a failure worth a toast.
 */
export const useRokuDevToolsStore = defineStore('rokuDevTools', () => {
  const api = useApi()

  const sgNodes = ref<SgNodesResult | null>(null)
  const sgLoading = ref(false)
  const sgError = ref<ApiError | null>(null)
  /** Index-path key ("0-3-1") of the node selected in the inspector; it is outlined on the video. */
  const sgSelectedKey = ref<string | null>(null)
  /** Key of the node under the pointer in the inspector; outlined instead of the selection while set. */
  const sgHoveredKey = ref<string | null>(null)
  /** The channel's UI coordinate space from the last capture that had a Scene node. */
  const sgUiSize = ref<SgSize | null>(null)

  /** Nodes from the root down to `key` in the current capture. */
  function sgPath(key: string | null): SgNode[] | null {
    return key && sgNodes.value ? pathForKey(sgNodes.value.nodes, key) : null
  }

  const sgSelectedPath = computed(() => sgPath(sgSelectedKey.value))
  /** What to outline on the video: the hovered node, else the selected one. */
  const sgHighlight = computed<SgHighlight | null>(() =>
    toHighlight(sgPath(sgHoveredKey.value) ?? sgSelectedPath.value, sgUiSize.value ?? DEFAULT_UI_SIZE),
  )

  const perfSamples = ref<ChanPerfSample[]>([])
  const perfError = ref<ApiError | null>(null)
  const perfPolling = ref(false)
  let perfTimer: ReturnType<typeof setTimeout> | null = null
  let perfGeneration = 0

  const registry = ref<RokuRegistry | null>(null)
  const registryLoading = ref(false)
  const registryError = ref<ApiError | null>(null)

  const base = (rokuId: number) => `/roku-devices/${rokuId}/dev-tools`

  async function loadSgNodes(rokuId: number, query: SgNodesQuery) {
    sgLoading.value = true
    sgError.value = null
    try {
      const previous = sgSelectedPath.value?.at(-1)
      sgNodes.value = await api.get<SgNodesResult>(`${base(rokuId)}/sgnodes`, {
        query: { scope: query.scope, nodeId: query.nodeId || undefined, sizes: query.sizes || undefined },
        silent: true,
      })
      sgUiSize.value = uiSize(sgNodes.value.nodes) ?? sgUiSize.value
      sgHoveredKey.value = null
      // Keys are index paths, so keep the selection only if the same node is still at that spot.
      const current = sgSelectedPath.value?.at(-1)
      if (!previous || !current || nodeSignature(previous) !== nodeSignature(current)) sgSelectedKey.value = null
    } catch (e) {
      sgError.value = e as ApiError
    } finally {
      sgLoading.value = false
    }
  }

  /** Polls chanperf every `intervalMs` (after each answer, so slow replies never overlap) until stopped. */
  function startPerf(rokuId: number, intervalMs = 1000) {
    stopPerf()
    const generation = ++perfGeneration
    perfPolling.value = true
    perfError.value = null

    const tick = async () => {
      try {
        const perf = await api.get<ChanPerf>(`${base(rokuId)}/chanperf`, { silent: true })
        if (generation !== perfGeneration) return
        perfError.value = null
        perfSamples.value = [...perfSamples.value, { at: Date.now(), perf }].slice(-PERF_HISTORY)
      } catch (e) {
        if (generation !== perfGeneration) return
        perfError.value = e as ApiError
      }
      // Keep polling after errors (slower): the user may enable dev mode or launch the dev channel.
      perfTimer = setTimeout(tick, perfError.value ? Math.max(intervalMs, 10_000) : intervalMs)
    }
    void tick()
  }

  function stopPerf() {
    perfGeneration++
    if (perfTimer) clearTimeout(perfTimer)
    perfTimer = null
    perfPolling.value = false
  }

  function clearPerf() {
    perfSamples.value = []
  }

  async function loadRegistry(rokuId: number, appId: string) {
    registryLoading.value = true
    registryError.value = null
    try {
      registry.value = await api.get<RokuRegistry>(`${base(rokuId)}/registry/${encodeURIComponent(appId)}`, {
        silent: true,
      })
    } catch (e) {
      registry.value = null
      registryError.value = e as ApiError
    } finally {
      registryLoading.value = false
    }
  }

  /** Forgets everything (e.g. when switching to another Roku). */
  function reset() {
    stopPerf()
    sgNodes.value = null
    sgError.value = null
    sgSelectedKey.value = null
    sgHoveredKey.value = null
    sgUiSize.value = null
    perfSamples.value = []
    perfError.value = null
    registry.value = null
    registryError.value = null
  }

  return {
    sgNodes,
    sgLoading,
    sgError,
    sgSelectedKey,
    sgHoveredKey,
    sgUiSize,
    sgSelectedPath,
    sgHighlight,
    perfSamples,
    perfError,
    perfPolling,
    registry,
    registryLoading,
    registryError,
    loadSgNodes,
    startPerf,
    stopPerf,
    clearPerf,
    loadRegistry,
    reset,
  }
})
