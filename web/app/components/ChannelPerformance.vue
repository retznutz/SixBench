<script setup lang="ts">
import { PERF_HISTORY } from '~/stores/rokuDevTools'
import type { ChanPerf } from '~/types/roku-dev-tools'

/** `active`: the tab is visible; polling runs only while it is. */
const props = defineProps<{ rokuId: number; active: boolean }>()

const devTools = useRokuDevToolsStore()

const cpuHover = ref<number | null>(null)
const memHover = ref<number | null>(null)

const samples = computed(() => devTools.perfSamples)
const latest = computed(() => samples.value.at(-1)?.perf ?? null)

const cpuTotal = (p: ChanPerf) =>
  p.cpuUserPercent == null && p.cpuSysPercent == null ? null : (p.cpuUserPercent ?? 0) + (p.cpuSysPercent ?? 0)

const cpuSeries = computed(() => samples.value.map((s) => cpuTotal(s.perf)))
const memSeries = computed(() => samples.value.map((s) => s.perf.memoryUsedBytes))

const cpuShown = computed(() => samples.value[cpuHover.value ?? -1] ?? samples.value.at(-1) ?? null)
const memShown = computed(() => samples.value[memHover.value ?? -1] ?? samples.value.at(-1) ?? null)

function bytes(n: number | null | undefined): string {
  if (n == null) return '—'
  const units = ['B', 'KB', 'MB', 'GB']
  let v = n
  let u = 0
  while (v >= 1024 && u < units.length - 1) {
    v /= 1024
    u++
  }
  return `${v.toFixed(v < 10 && u > 0 ? 1 : 0)} ${units[u]}`
}

function percent(n: number | null | undefined): string {
  return n == null ? '—' : `${n.toFixed(1)}%`
}

function ago(at: number): string {
  const s = Math.round((Date.now() - at) / 1000)
  return s <= 1 ? 'now' : `${s}s ago`
}

const memoryRows = computed(() => {
  const p = latest.value
  if (!p) return []
  return [
    { label: 'Resident', value: p.memoryResidentBytes },
    { label: 'Anonymous', value: p.memoryAnonBytes },
    { label: 'File-backed', value: p.memoryFileBytes },
    { label: 'Shared', value: p.memorySharedBytes },
    { label: 'Swap', value: p.memorySwapBytes },
  ]
})

function toggle() {
  if (devTools.perfPolling) devTools.stopPerf()
  else devTools.startPerf(props.rokuId)
}

watch(
  () => [props.active, props.rokuId] as const,
  ([active, id], previous) => {
    if (previous && previous[1] !== id) devTools.clearPerf()
    if (active) devTools.startPerf(id)
    else devTools.stopPerf()
  },
  { immediate: true },
)
onBeforeUnmount(() => devTools.stopPerf())
</script>

<template>
  <div class="space-y-3">
    <div class="flex flex-wrap items-center gap-2 text-xs text-surface-500">
      <span v-if="latest">
        App <span class="font-mono text-surface-300">{{ latest.appId ?? '?' }}</span>
        <template v-if="latest.processId"> · PID {{ latest.processId }}</template>
      </span>
      <span v-else-if="devTools.perfPolling && !devTools.perfError">Waiting for the first sample…</span>
      <span class="flex-1" />
      <Button
        :label="devTools.perfPolling ? 'Pause' : 'Resume'"
        :icon="devTools.perfPolling ? 'pi pi-pause' : 'pi pi-play'"
        text
        size="small"
        @click="toggle"
      />
      <Button
        label="Clear"
        icon="pi pi-trash"
        text
        size="small"
        :disabled="!samples.length"
        @click="devTools.clearPerf()"
      />
    </div>

    <Message v-if="devTools.perfError" severity="warn" size="small">
      <strong>{{ devTools.perfError.title }}.</strong> {{ devTools.perfError.userMessage }}
      <span v-if="devTools.perfPolling" class="block text-xs opacity-80">Retrying every 10 seconds.</span>
    </Message>

    <div class="grid gap-3 sm:grid-cols-2">
      <section class="rounded-lg border border-surface-800 p-3" aria-label="CPU">
        <p class="text-xs text-surface-500">CPU</p>
        <p class="text-2xl font-semibold tabular-nums">{{ percent(cpuShown ? cpuTotal(cpuShown.perf) : null) }}</p>
        <p class="mb-2 text-xs tabular-nums text-surface-500">
          <template v-if="cpuShown">
            user {{ percent(cpuShown.perf.cpuUserPercent) }} · sys {{ percent(cpuShown.perf.cpuSysPercent) }} ·
            {{ ago(cpuShown.at) }}
          </template>
          <template v-else>&nbsp;</template>
        </p>
        <Sparkline
          :values="cpuSeries"
          :capacity="PERF_HISTORY"
          zero-based
          :label="`CPU over the last ${samples.length} samples`"
          @hover="cpuHover = $event"
        />
      </section>

      <section class="rounded-lg border border-surface-800 p-3" aria-label="Memory">
        <p class="text-xs text-surface-500">Memory used</p>
        <p class="text-2xl font-semibold tabular-nums">{{ bytes(memShown?.perf.memoryUsedBytes) }}</p>
        <p class="mb-2 text-xs tabular-nums text-surface-500">
          <template v-if="memShown">{{ ago(memShown.at) }}</template>
          <template v-else>&nbsp;</template>
        </p>
        <Sparkline
          :values="memSeries"
          :capacity="PERF_HISTORY"
          :label="`Memory used over the last ${samples.length} samples`"
          @hover="memHover = $event"
        />
      </section>
    </div>

    <dl v-if="memoryRows.length" class="grid grid-cols-2 gap-x-6 gap-y-1 text-xs sm:grid-cols-5">
      <div v-for="row in memoryRows" :key="row.label">
        <dt class="text-surface-500">{{ row.label }}</dt>
        <dd class="tabular-nums text-surface-200">{{ bytes(row.value) }}</dd>
      </div>
    </dl>
  </div>
</template>
