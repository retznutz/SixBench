<script setup lang="ts">
import { ApiError } from '~/types/api-error'
import type { CaptureDevice } from '~/types/capture-device'
import type { UpsertEncoderLinkRequest } from '~/types/encoder-link'

const visible = defineModel<boolean>('visible', { required: true })
const props = defineProps<{ device: CaptureDevice | null }>()

const devices = useCaptureDevicesStore()
const rokus = useRokuDevicesStore()
const links = useEncoderLinksStore()
const toast = useToast()
const confirm = useConfirm()

const form = reactive<Required<UpsertEncoderLinkRequest>>({
  displayName: '',
  videoInput: null,
  audioInput: null,
  rokuDeviceId: null,
  allowDeviceAudio: false,
  frameRate: null,
  videoSize: null,
  pixelFormat: null,
})
const errors = ref<Record<string, string[]>>({})
const showAdvanced = ref(false)

const rokuOptions = computed(() => [
  { label: 'None', value: null },
  ...rokus.devices.map((r) => ({ label: `${r.friendlyName} (${r.ipAddress})`, value: r.id })),
])

const audioOptions = computed(() => [
  { label: 'Auto-detect', value: null },
  ...devices.audioInputs.map((a) => ({ label: a.name, value: a.input })),
])

watch(
  () => [visible.value, props.device] as const,
  ([open, device]) => {
    if (!open || !device) return
    const link = device.link
    form.displayName = link?.displayName ?? device.name
    form.videoInput = link?.videoInput ?? null
    form.audioInput = link?.audioInput ?? null
    form.rokuDeviceId = link?.rokuDeviceId ?? null
    form.allowDeviceAudio = link?.allowDeviceAudio ?? false
    form.frameRate = link?.frameRate ?? null
    form.videoSize = link?.videoSize ?? null
    form.pixelFormat = link?.pixelFormat ?? null
    showAdvanced.value = !!(link?.frameRate || link?.videoSize || link?.pixelFormat)
    errors.value = {}
    void devices.fetchAudioInputs().catch(() => undefined)
  },
  { immediate: true },
)

function fieldError(name: string): string | undefined {
  const key = Object.keys(errors.value).find((k) => k.toLowerCase() === name.toLowerCase())
  return key ? errors.value[key]?.join(' ') : undefined
}

async function save() {
  if (!props.device) return
  errors.value = {}
  try {
    await links.save(props.device.id, {
      ...form,
      videoSize: form.videoSize?.trim() || null,
      pixelFormat: form.pixelFormat?.trim() || null,
    })
    toast.add({ severity: 'success', summary: 'Encoder saved', life: 2500 })
    visible.value = false
  } catch (e) {
    if (e instanceof ApiError && e.errors) errors.value = e.errors
  }
}

function removeLink() {
  const device = props.device
  if (!device?.link) return
  confirm.require({
    header: 'Remove link?',
    message: `Forget the saved settings for ${device.link.displayName}?`,
    icon: 'pi pi-exclamation-triangle',
    acceptProps: { label: 'Remove', severity: 'danger' },
    rejectProps: { label: 'Cancel', severity: 'secondary', outlined: true },
    accept: async () => {
      await links.remove(device.id)
      visible.value = false
    },
  })
}
</script>

<template>
  <Dialog v-model:visible="visible" modal header="Configure encoder" class="w-[min(32rem,calc(100vw-2rem))]">
    <form v-if="device" class="flex flex-col gap-4" @submit.prevent="save">
      <p class="-mt-2 text-xs break-words text-zinc-500" :title="device.stableId">{{ device.name }}</p>

      <div class="flex flex-col gap-1">
        <label for="link-name" class="text-sm">Name</label>
        <InputText
          id="link-name"
          v-model="form.displayName"
          maxlength="200"
          required
          :invalid="!!fieldError('displayName')"
        />
        <small v-if="fieldError('displayName')" class="text-red-400">{{ fieldError('displayName') }}</small>
      </div>

      <div class="flex flex-col gap-1">
        <label for="link-roku" class="text-sm">Roku connected to this encoder</label>
        <Select
          v-model="form.rokuDeviceId"
          input-id="link-roku"
          :options="rokuOptions"
          option-label="label"
          option-value="value"
          :loading="rokus.loading"
          placeholder="None"
        />
        <small v-if="rokus.devices.length === 0" class="text-zinc-500"
          >No Rokus saved yet — discover or add one first.</small
        >
      </div>

      <div class="flex flex-col gap-1">
        <label for="link-audio" class="text-sm">Audio input</label>
        <Select
          v-model="form.audioInput"
          input-id="link-audio"
          :options="audioOptions"
          option-label="label"
          option-value="value"
        />
        <small class="text-zinc-500">
          Auto-detect pairs the encoder's own audio device{{ device.audioInput ? ` (${device.audioInput})` : '' }}.
        </small>
      </div>

      <label class="flex items-center justify-between gap-3 text-sm">
        <span>Allow device audio in the browser</span>
        <ToggleSwitch v-model="form.allowDeviceAudio" input-id="link-allow-audio" />
      </label>

      <button
        type="button"
        class="flex items-center gap-2 self-start text-sm text-zinc-400 hover:text-zinc-200"
        @click="showAdvanced = !showAdvanced"
      >
        <i :class="showAdvanced ? 'pi pi-chevron-down' : 'pi pi-chevron-right'" class="text-xs" aria-hidden="true" />
        Capture settings
      </button>
      <div v-if="showAdvanced" class="grid gap-3 sm:grid-cols-3">
        <div class="flex flex-col gap-1">
          <label for="link-fps" class="text-xs text-zinc-400">Frame rate</label>
          <InputNumber
            v-model="form.frameRate"
            input-id="link-fps"
            :min="1"
            :max="240"
            :max-fraction-digits="3"
            placeholder="30"
            fluid
            :invalid="!!fieldError('frameRate')"
          />
        </div>
        <div class="flex flex-col gap-1">
          <label for="link-size" class="text-xs text-zinc-400">Video size</label>
          <InputText
            id="link-size"
            v-model="form.videoSize"
            placeholder="1920x1080"
            :invalid="!!fieldError('videoSize')"
          />
        </div>
        <div class="flex flex-col gap-1">
          <label for="link-pixfmt" class="text-xs text-zinc-400">Pixel format</label>
          <InputText
            id="link-pixfmt"
            v-model="form.pixelFormat"
            placeholder="uyvy422"
            :invalid="!!fieldError('pixelFormat')"
          />
        </div>
        <small
          v-for="name in ['frameRate', 'videoSize', 'pixelFormat']"
          v-show="fieldError(name)"
          :key="name"
          class="text-red-400 sm:col-span-3"
        >
          {{ fieldError(name) }}
        </small>
        <small class="text-zinc-500 sm:col-span-3">
          Leave blank for defaults. Set these if ffmpeg reports the device does not support the requested mode.
        </small>
      </div>

      <div class="mt-2 flex items-center gap-2">
        <Button
          v-if="device.link"
          type="button"
          label="Remove"
          icon="pi pi-trash"
          severity="danger"
          text
          @click="removeLink"
        />
        <span class="flex-1" />
        <Button type="button" label="Cancel" severity="secondary" outlined @click="visible = false" />
        <Button type="submit" label="Save" icon="pi pi-check" :loading="links.saving" />
      </div>
    </form>
  </Dialog>
</template>
