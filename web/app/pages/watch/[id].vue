<script setup lang="ts">
import type { CaptureDevice } from '~/types/capture-device'
import type { DecoderKind } from '~/types/stream-protocol'
import type { RokuKey } from '~/types/remote'
import { KEYBOARD_SHORTCUTS } from '~/composables/useKeyboardShortcuts'

const route = useRoute()
const toast = useToast()
const devices = useCaptureDevicesStore()
const stream = useStreamStore()
const remote = useRemoteStore()

const id = computed(() => String(route.params.id))
const decoder = computed<DecoderKind>(() => (route.query.decoder === 'mse' ? 'mse' : 'webcodecs'))
const device = ref<CaptureDevice | null>(devices.byId(id.value) ?? null)
const surface = ref<{
  root: HTMLElement | null
  canvas: HTMLCanvasElement | null
  video: HTMLVideoElement | null
  screenshot: () => Promise<void>
  thumbnail: () => Promise<Blob>
} | null>(null)
const surfaceRoot = computed(() => surface.value?.root ?? null)
const textOpen = ref(false)
const devToolsOpen = ref(readDevToolsPref())

/** Remembers whether the developer tools were open; storage may be unavailable. */
function readDevToolsPref() {
  try {
    return localStorage.getItem('sixbench.devToolsOpen') === '1'
  } catch {
    return false
  }
}

watch(devToolsOpen, (open) => {
  try {
    localStorage.setItem('sixbench.devToolsOpen', open ? '1' : '0')
  } catch {
    // Not persisted; the toggle still works for this visit.
  }
})

const name = computed(() => (device.value ? devices.displayName(device.value) : 'Encoder'))
const rokuId = computed(() => device.value?.link?.rokuDeviceId ?? null)
const rokuName = computed(() => device.value?.link?.rokuDevice?.friendlyName ?? null)
const rokuDevPage = computed(() => {
  const roku = device.value?.link?.rokuDevice
  return roku ? rokuDevPageUrl(roku) : null
})

useHead({ title: () => `${name.value} · SixBench` })

const statusTag = computed(() => {
  switch (stream.status) {
    case 'live':
      return { value: 'Live', severity: 'success' }
    case 'connecting':
    case 'reconnecting':
      return { value: stream.status === 'connecting' ? 'Connecting' : 'Reconnecting', severity: 'warn' }
    case 'error':
    case 'unsupported':
      return { value: 'Error', severity: 'danger' }
    default:
      return { value: 'Idle', severity: 'secondary' }
  }
})

function start() {
  const s = surface.value
  if (!s) return
  stream.connect(id.value, decoder.value, { canvas: s.canvas, video: s.video })
}

/** Moves the developer tools into their own browser window. Runs inside the click, so no awaiting before the open. */
function popOutDevTools(tab: string) {
  if (rokuId.value == null) return
  if (openDevToolsWindow(rokuId.value, tab)) {
    devToolsOpen.value = false
  } else {
    toast.add({
      severity: 'warn',
      summary: 'Pop-up blocked',
      detail: 'Allow pop-ups for this site to open developer tools in a new window.',
      life: 6000,
    })
  }
}

/** How long the video must have been playing before the first thumbnail, so it isn't a black or half-drawn frame. */
const THUMBNAIL_DELAY_MS = 3000
const hasFrames = computed(() => stream.status === 'live' && (stream.stats?.width ?? 0) > 0)
let thumbnailTimer: ReturnType<typeof setTimeout> | undefined
/** One attempt per encoder per visit; a failure is retried on the next visit. */
let thumbnailTriedFor: string | null = null

// The first time an encoder plays, save a frame as its thumbnail for the Encoders page.
watch([hasFrames, () => device.value?.id, () => device.value?.thumbnailUpdatedUtc], ([live]) => {
  clearTimeout(thumbnailTimer)
  const current = device.value
  if (live && current && !current.thumbnailUpdatedUtc && thumbnailTriedFor !== current.id) {
    thumbnailTimer = setTimeout(saveThumbnail, THUMBNAIL_DELAY_MS)
  }
})

async function saveThumbnail() {
  const current = device.value
  const s = surface.value
  if (!current || !s || current.thumbnailUpdatedUtc || !hasFrames.value || thumbnailTriedFor === current.id) return
  thumbnailTriedFor = current.id
  try {
    const updated = await devices.uploadThumbnail(current.id, await s.thumbnail())
    if (device.value?.id === updated.id) device.value = updated
  } catch {
    // Not essential: the Encoders page keeps its placeholder until a later visit succeeds.
  }
}

function sendKey(key: RokuKey) {
  if (rokuId.value != null) remote.sendKey(rokuId.value, key).catch(() => undefined)
}

useKeyboardShortcuts(surfaceRoot, {
  onKey: sendKey,
  onTextEntry: () => (textOpen.value = true),
  onScreenshot: () => surface.value?.screenshot(),
})

onMounted(async () => {
  await nextTick()
  start()
  surface.value?.root?.focus()
  device.value = await devices.fetchOne(id.value).catch(() => device.value)
})

watch([id, decoder], async () => {
  device.value = devices.byId(id.value) ?? null
  await nextTick()
  start()
  device.value = await devices.fetchOne(id.value).catch(() => device.value)
})

onBeforeUnmount(() => {
  clearTimeout(thumbnailTimer)
  stream.disconnect()
})
</script>

<template>
  <div class="space-y-4">
    <div class="flex flex-wrap items-center gap-3">
      <NuxtLink to="/" aria-label="Back to encoders">
        <Button icon="pi pi-arrow-left" text rounded severity="secondary" aria-label="Back to encoders" />
      </NuxtLink>
      <h1 class="min-w-0 truncate text-xl font-bold tracking-tight">{{ name }}</h1>
      <Tag :value="statusTag.value" :severity="statusTag.severity" />
      <span class="flex-1" />
      <Button
        label="Dev tools"
        icon="pi pi-wrench"
        size="small"
        :text="!devToolsOpen"
        :severity="devToolsOpen ? 'primary' : 'secondary'"
        :disabled="rokuId == null"
        :aria-pressed="devToolsOpen"
        :title="
          rokuId == null ? 'Link a Roku to this encoder to use developer tools' : 'SceneGraph, performance and registry'
        "
        @click="devToolsOpen = !devToolsOpen"
      />
      <div class="flex items-center gap-1 text-xs text-surface-500">
        Decoder:
        <NuxtLink
          :to="{ query: { ...route.query, decoder: undefined } }"
          class="rounded px-2 py-1"
          :class="decoder === 'webcodecs' ? 'bg-surface-800 text-surface-100' : 'hover:text-surface-300'"
        >
          WebCodecs
        </NuxtLink>
        <NuxtLink
          :to="{ query: { ...route.query, decoder: 'mse' } }"
          class="rounded px-2 py-1"
          :class="decoder === 'mse' ? 'bg-surface-800 text-surface-100' : 'hover:text-surface-300'"
        >
          MSE
        </NuxtLink>
      </div>
    </div>

    <div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_16rem]">
      <div class="space-y-3">
        <VideoSurface ref="surface" :key="decoder" :decoder="decoder" :name="rokuName ?? name" @retry="start" />
        <LazyRokuDevTools
          v-if="devToolsOpen && rokuId != null"
          :roku-id="rokuId"
          :dev-page-url="rokuDevPage"
          @close="devToolsOpen = false"
          @pop-out="popOutDevTools"
        />
      </div>

      <aside class="space-y-4">
        <RemoteControl :roku-id="rokuId" :roku-name="rokuName" @open-text="textOpen = true" />
        <AudioControls />
        <Panel header="Keyboard shortcuts" toggleable collapsed class="text-sm">
          <dl class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-xs">
            <template v-for="s in KEYBOARD_SHORTCUTS" :key="s.roku">
              <dt>
                <kbd class="rounded bg-surface-800 px-1.5 py-0.5 font-mono">{{ s.label }}</kbd>
              </dt>
              <dd class="text-surface-400">{{ s.roku }}</dd>
            </template>
            <dt><kbd class="rounded bg-surface-800 px-1.5 py-0.5 font-mono">T</kbd></dt>
            <dd class="text-surface-400">Type text</dd>
            <dt><kbd class="rounded bg-surface-800 px-1.5 py-0.5 font-mono">S</kbd></dt>
            <dd class="text-surface-400">Screenshot</dd>
          </dl>
        </Panel>
      </aside>
    </div>

    <LazyTextEntryDialog v-model:visible="textOpen" :roku-id="rokuId" />
  </div>
</template>
