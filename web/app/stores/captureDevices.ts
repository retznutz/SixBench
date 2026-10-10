import { defineStore } from 'pinia'
import type { AudioInput, CaptureDevice } from '~/types/capture-device'
import type { EncoderLink } from '~/types/encoder-link'

export const useCaptureDevicesStore = defineStore('captureDevices', () => {
  const api = useApi()
  const config = useRuntimeConfig()

  const devices = ref<CaptureDevice[]>([])
  const audioInputs = ref<AudioInput[]>([])
  const loading = ref(false)
  const loaded = ref(false)

  const connected = computed(() => devices.value.filter((d) => d.isConnected))

  function byId(id: string): CaptureDevice | undefined {
    return devices.value.find((d) => d.id === id)
  }

  /** Display name: the saved link's name, else the OS device name. */
  function displayName(device: CaptureDevice): string {
    return device.link?.displayName || device.name
  }

  async function fetchAll(refresh = false) {
    loading.value = true
    try {
      devices.value = await api.get<CaptureDevice[]>('/capture-devices', { query: { refresh } })
      loaded.value = true
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: string) {
    const device = await api.get<CaptureDevice>(`/capture-devices/${encodeURIComponent(id)}`)
    const index = devices.value.findIndex((d) => d.id === id)
    if (index >= 0) devices.value[index] = device
    else devices.value.push(device)
    return device
  }

  /** Image URL for the device's thumbnail, or null if it has none. Changes when a new one is saved. */
  function thumbnailUrl(device: CaptureDevice): string | null {
    if (!device.thumbnailUpdatedUtc) return null
    const version = encodeURIComponent(device.thumbnailUpdatedUtc)
    return `${config.public.apiBase}/capture-devices/${encodeURIComponent(device.id)}/thumbnail?v=${version}`
  }

  /** Saves a frame as the device's thumbnail. Errors are left to the caller. */
  async function uploadThumbnail(id: string, image: Blob) {
    const body = new FormData()
    body.append('image', image, 'thumbnail.jpg')
    const device = await api.put<CaptureDevice>(`/capture-devices/${encodeURIComponent(id)}/thumbnail`, {
      body,
      silent: true,
    })
    const index = devices.value.findIndex((d) => d.id === id)
    if (index >= 0) devices.value[index] = device
    return device
  }

  async function fetchAudioInputs() {
    audioInputs.value = await api.get<AudioInput[]>('/capture-devices/audio-inputs')
  }

  /** Applies a saved or deleted link to the cached device list. */
  function applyLink(id: string, link: EncoderLink | null) {
    const device = byId(id)
    if (!device) return
    device.link = link
    if (link?.audioInput) device.audioInput = link.audioInput
  }

  return {
    devices,
    audioInputs,
    loading,
    loaded,
    connected,
    byId,
    displayName,
    fetchAll,
    fetchOne,
    thumbnailUrl,
    uploadThumbnail,
    fetchAudioInputs,
    applyLink,
  }
})
