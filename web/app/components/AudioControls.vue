<script setup lang="ts">
import { DeviceAudioPlayer } from '~/lib/stream/DeviceAudioPlayer'

const stream = useStreamStore()
const outputs = ref<{ label: string; value: string }[]>([{ label: 'System default', value: 'default' }])
const canChooseOutput = computed(() => import.meta.client && DeviceAudioPlayer.supportsOutputSelection())

const audioOn = computed({
  get: () => stream.audioStatus === 'on' || stream.audioStatus === 'starting',
  set: (value: boolean) => {
    if (value) void stream.startAudio()
    else void stream.stopAudio()
  },
})

const audioDisabledReason = computed(() => {
  if (!stream.audioSupported) return 'This browser cannot decode Opus with WebCodecs.'
  if (stream.status !== 'live') return 'Available once the stream is live.'
  if (!stream.grant.device) return 'Enable device audio for this encoder in Settings.'
  return null
})

async function loadOutputs() {
  if (!navigator.mediaDevices?.enumerateDevices) return
  const devices = await navigator.mediaDevices.enumerateDevices()
  const list = devices
    .filter((d) => d.kind === 'audiooutput' && d.deviceId !== 'default')
    .map((d, i) => ({ label: d.label || `Output ${i + 1}`, value: d.deviceId }))
  outputs.value = [{ label: 'System default', value: 'default' }, ...list]
}

onMounted(() => {
  if (!canChooseOutput.value) return
  void loadOutputs()
  navigator.mediaDevices?.addEventListener('devicechange', loadOutputs)
})
onBeforeUnmount(() => navigator.mediaDevices?.removeEventListener('devicechange', loadOutputs))
</script>

<template>
  <section class="space-y-4 rounded-xl border border-surface-800 bg-surface-900/60 p-4" aria-label="Audio">
    <h2 class="text-sm font-medium text-surface-300">Audio</h2>

    <label class="flex items-center justify-between gap-3 text-sm" :title="audioDisabledReason ?? undefined">
      <span class="flex items-center gap-2">
        <i class="pi pi-volume-up text-surface-400" aria-hidden="true" />
        Device audio
        <ProgressSpinner v-if="stream.audioStatus === 'starting'" style="width: 14px; height: 14px" stroke-width="6" />
      </span>
      <ToggleSwitch v-model="audioOn" :disabled="!!audioDisabledReason" input-id="device-audio" />
    </label>
    <p v-if="audioDisabledReason && stream.status === 'live'" class="-mt-2 text-xs text-surface-500">
      {{ audioDisabledReason }}
    </p>
    <Message v-if="stream.audioMessage" severity="warn" size="small">{{ stream.audioMessage }}</Message>

    <div v-if="canChooseOutput" class="flex flex-col gap-1">
      <label for="audio-output" class="text-xs text-surface-500">Output device</label>
      <Select
        input-id="audio-output"
        :model-value="stream.outputDeviceId"
        :options="outputs"
        option-label="label"
        option-value="value"
        size="small"
        class="w-full"
        @update:model-value="(v: string) => stream.setOutputDevice(v)"
        @show="loadOutputs"
      />
    </div>

    <label
      v-tooltip.top="'Not supported by Roku: HDMI capture is one-way and ECP has no audio input.'"
      class="flex items-center justify-between gap-3 text-sm text-surface-500"
    >
      <span class="flex items-center gap-2">
        <i class="pi pi-microphone" aria-hidden="true" />
        Microphone
      </span>
      <ToggleSwitch :model-value="false" disabled input-id="mic" aria-describedby="mic-note" />
    </label>
    <p id="mic-note" class="-mt-2 text-xs text-surface-600">Not supported by Roku.</p>
  </section>
</template>
