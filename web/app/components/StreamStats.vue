<script setup lang="ts">
const stream = useStreamStore()

const rows = computed(() => {
  const s = stream.stats
  return [
    ['Decoder', s?.decoder ?? stream.decoder],
    ['Codec', s?.codec ?? '—'],
    ['Resolution', s && s.width ? `${s.width}×${s.height}` : '—'],
    ['FPS', s ? s.fps.toFixed(1) : '—'],
    ['Bitrate', s ? `${(s.bitrateKbps / 1000).toFixed(2)} Mbps` : '—'],
    ['Jitter', s ? `${s.jitterMs.toFixed(1)} ms` : '—'],
    ['Dropped', s ? String(s.droppedFrames) : '—'],
    ['Session', stream.sessionId ?? '—'],
  ] as const
})
</script>

<template>
  <dl
    class="grid grid-cols-[auto_auto] gap-x-3 gap-y-0.5 rounded-lg bg-black/75 px-3 py-2 font-mono text-[11px] text-zinc-300"
  >
    <template v-for="[label, value] in rows" :key="label">
      <dt class="text-zinc-500">{{ label }}</dt>
      <dd>{{ value }}</dd>
    </template>
  </dl>
</template>
