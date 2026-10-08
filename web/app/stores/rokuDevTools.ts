import { defineStore } from 'pinia'
import type { ApiError } from '~/types/api-error'
import type { ChanPerf, ChanPerfSample, RokuRegistry, SgNodesQuery, SgNodesResult } from '~/types/roku-dev-tools'

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
      sgNodes.value = await api.get<SgNodesResult>(`${base(rokuId)}/sgnodes`, {
        query: { scope: query.scope, nodeId: query.nodeId || undefined, sizes: query.sizes || undefined },
        silent: true,
      })
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
    perfSamples.value = []
    perfError.value = null
    registry.value = null
    registryError.value = null
  }

  return {
    sgNodes,
    sgLoading,
    sgError,
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
