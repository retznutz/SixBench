<script setup lang="ts">
import type { DecoderKind } from '~/types/stream-protocol'
import { captureFrame, captureThumbnail, downloadBlob, snapshotFileName } from '~/lib/stream/snapshot'

/** `name` labels screenshot files. */
const props = defineProps<{ decoder: DecoderKind; name?: string }>()
const emit = defineEmits<{ retry: [] }>()

const stream = useStreamStore()
const root = ref<HTMLElement | null>(null)
const canvas = ref<HTMLCanvasElement | null>(null)
const video = ref<HTMLVideoElement | null>(null)
const toast = useToast()
const showStats = ref(false)
const capturing = ref(false)
const focused = ref(false)

const overlay = computed(() => {
  switch (stream.status) {
    case 'idle':
    case 'connecting':
      return { icon: 'spinner', title: 'Connecting…', detail: null }
    case 'reconnecting':
      return { icon: 'spinner', title: 'Reconnecting…', detail: stream.message }
    case 'error':
      return { icon: 'pi pi-exclamation-triangle', title: 'Stream error', detail: stream.message, retry: true }
    case 'unsupported':
      return { icon: 'pi pi-ban', title: 'Browser not supported', detail: stream.message }
    case 'ended':
      return { icon: 'pi pi-stop-circle', title: 'Stream ended', detail: stream.message, retry: true }
    default:
      return stream.stats && stream.stats.width > 0
        ? null
        : { icon: 'spinner', title: 'Waiting for video…', detail: null }
  }
})

async function toggleFullscreen() {
  if (!root.value) return
  if (document.fullscreenElement) await document.exitFullscreen()
  else await root.value.requestFullscreen().catch(() => undefined)
  root.value.focus()
}

const canScreenshot = computed(() => stream.status === 'live' && (stream.stats?.width ?? 0) > 0)

/** Saves the frame on screen as a PNG at the stream's native resolution. */
async function screenshot() {
  const source = props.decoder === 'webcodecs' ? canvas.value : video.value
  if (!source || capturing.value) return
  capturing.value = true
  try {
    const fileName = snapshotFileName(props.name ?? 'sixbench')
    downloadBlob(await captureFrame(source), fileName)
    toast.add({ severity: 'success', summary: 'Screenshot saved', detail: fileName, life: 3000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Screenshot failed', detail: (e as Error).message, life: 6000 })
  } finally {
    capturing.value = false
  }
}

/** The frame on screen as a small JPEG, for the encoder's thumbnail. */
function thumbnail(): Promise<Blob> {
  const source = props.decoder === 'webcodecs' ? canvas.value : video.value
  return source ? captureThumbnail(source) : Promise.reject(new Error('The video is not ready.'))
}

defineExpose({ root, canvas, video, screenshot, thumbnail })
</script>

<template>
  <div
    ref="root"
    tabindex="0"
    class="group relative aspect-video w-full overflow-hidden rounded-xl bg-black outline-none ring-2 ring-transparent transition-shadow focus-visible:ring-primary"
    :class="{ 'ring-primary/70': focused }"
    aria-label="Roku video. Focus to control with the keyboard."
    @focus="focused = true"
    @blur="focused = false"
    @dblclick="toggleFullscreen"
  >
    <canvas v-if="props.decoder === 'webcodecs'" ref="canvas" class="size-full object-contain" />
    <video v-else ref="video" class="size-full object-contain" muted playsinline autoplay />

    <div
      v-if="overlay"
      class="absolute inset-0 grid place-items-center bg-black/70 p-6 text-center"
      role="status"
      aria-live="polite"
    >
      <div class="flex max-w-md flex-col items-center gap-3">
        <ProgressSpinner v-if="overlay.icon === 'spinner'" style="width: 40px; height: 40px" stroke-width="4" />
        <i v-else :class="overlay.icon" class="text-3xl text-surface-300" aria-hidden="true" />
        <p class="font-medium text-surface-100">{{ overlay.title }}</p>
        <p v-if="overlay.detail" class="max-h-32 overflow-auto whitespace-pre-line text-xs text-surface-400">
          {{ overlay.detail }}
        </p>
        <Button v-if="overlay.retry" label="Retry" icon="pi pi-refresh" size="small" @click="emit('retry')" />
      </div>
    </div>

    <div
      class="absolute inset-x-0 top-0 flex items-center justify-end gap-1 bg-gradient-to-b from-black/60 to-transparent p-2 opacity-0 transition-opacity group-focus-within:opacity-100 group-hover:opacity-100"
    >
      <Button
        v-tooltip.bottom="'Stats'"
        icon="pi pi-chart-bar"
        text
        rounded
        size="small"
        severity="contrast"
        :aria-pressed="showStats"
        aria-label="Toggle stream stats"
        @click.stop="showStats = !showStats"
      />
      <Button
        v-tooltip.bottom="'Screenshot'"
        icon="pi pi-camera"
        text
        rounded
        size="small"
        severity="contrast"
        aria-label="Save a screenshot"
        :disabled="!canScreenshot"
        :loading="capturing"
        @click.stop="screenshot"
      />
      <Button
        v-tooltip.bottom="'Resync video'"
        icon="pi pi-refresh"
        text
        rounded
        size="small"
        severity="contrast"
        aria-label="Request keyframe"
        @click.stop="stream.requestKeyframe()"
      />
      <Button
        v-tooltip.bottom="'Fullscreen'"
        icon="pi pi-expand"
        text
        rounded
        size="small"
        severity="contrast"
        aria-label="Toggle fullscreen"
        @click.stop="toggleFullscreen"
      />
    </div>

    <LazyStreamStats v-if="showStats" class="absolute bottom-2 left-2" />

    <p
      v-if="stream.status === 'live' && !focused"
      class="pointer-events-none absolute bottom-2 right-2 rounded bg-black/60 px-2 py-1 text-xs text-surface-400"
    >
      Click the video to use keyboard controls
    </p>
  </div>
</template>
