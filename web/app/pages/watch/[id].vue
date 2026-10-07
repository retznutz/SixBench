<script setup lang="ts">
import type { CaptureDevice } from '~/types/capture-device'
import type { DecoderKind } from '~/types/stream-protocol'
import type { RokuKey } from '~/types/remote'
import { KEYBOARD_SHORTCUTS } from '~/composables/useKeyboardShortcuts'

const route = useRoute()
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
} | null>(null)
const surfaceRoot = computed(() => surface.value?.root ?? null)
const textOpen = ref(false)

const name = computed(() => (device.value ? devices.displayName(device.value) : 'Encoder'))
const rokuId = computed(() => device.value?.link?.rokuDeviceId ?? null)
const rokuName = computed(() => device.value?.link?.rokuDevice?.friendlyName ?? null)

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

function sendKey(key: RokuKey) {
  if (rokuId.value != null) remote.sendKey(rokuId.value, key).catch(() => undefined)
}

useKeyboardShortcuts(surfaceRoot, { onKey: sendKey, onTextEntry: () => (textOpen.value = true) })

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

onBeforeUnmount(() => stream.disconnect())
</script>

<template>
  <div class="space-y-4">
    <div class="flex flex-wrap items-center gap-3">
      <NuxtLink to="/" aria-label="Back to encoders">
        <Button icon="pi pi-arrow-left" text rounded severity="secondary" aria-label="Back to encoders" />
      </NuxtLink>
      <h1 class="min-w-0 truncate text-xl font-semibold tracking-tight">{{ name }}</h1>
      <Tag :value="statusTag.value" :severity="statusTag.severity" />
      <span class="flex-1" />
      <div class="flex items-center gap-1 text-xs text-zinc-500">
        Decoder:
        <NuxtLink
          :to="{ query: { ...route.query, decoder: undefined } }"
          class="rounded px-2 py-1"
          :class="decoder === 'webcodecs' ? 'bg-zinc-800 text-zinc-100' : 'hover:text-zinc-300'"
        >
          WebCodecs
        </NuxtLink>
        <NuxtLink
          :to="{ query: { ...route.query, decoder: 'mse' } }"
          class="rounded px-2 py-1"
          :class="decoder === 'mse' ? 'bg-zinc-800 text-zinc-100' : 'hover:text-zinc-300'"
        >
          MSE
        </NuxtLink>
      </div>
    </div>

    <div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_20rem]">
      <div class="space-y-3">
        <VideoSurface ref="surface" :key="decoder" :decoder="decoder" @retry="start" />
      </div>

      <aside class="space-y-4">
        <RemoteControl :roku-id="rokuId" :roku-name="rokuName" @open-text="textOpen = true" />
        <AudioControls />
        <Panel header="Keyboard shortcuts" toggleable collapsed class="text-sm">
          <dl class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-xs">
            <template v-for="s in KEYBOARD_SHORTCUTS" :key="s.roku">
              <dt>
                <kbd class="rounded bg-zinc-800 px-1.5 py-0.5 font-mono">{{ s.label }}</kbd>
              </dt>
              <dd class="text-zinc-400">{{ s.roku }}</dd>
            </template>
            <dt><kbd class="rounded bg-zinc-800 px-1.5 py-0.5 font-mono">T</kbd></dt>
            <dd class="text-zinc-400">Type text</dd>
          </dl>
        </Panel>
      </aside>
    </div>

    <LazyTextEntryDialog v-model:visible="textOpen" :roku-id="rokuId" />
  </div>
</template>
