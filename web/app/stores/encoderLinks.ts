import { defineStore } from 'pinia'
import type { EncoderLink, UpsertEncoderLinkRequest } from '~/types/encoder-link'
import { useCaptureDevicesStore } from './captureDevices'

export const useEncoderLinksStore = defineStore('encoderLinks', () => {
  const api = useApi()
  const devices = useCaptureDevicesStore()
  const saving = ref(false)

  async function save(deviceId: string, request: UpsertEncoderLinkRequest) {
    saving.value = true
    try {
      const link = await api.put<EncoderLink>(`/encoder-links/${encodeURIComponent(deviceId)}`, { body: request })
      devices.applyLink(deviceId, link)
      return link
    } finally {
      saving.value = false
    }
  }

  async function remove(deviceId: string) {
    saving.value = true
    try {
      await api.del(`/encoder-links/${encodeURIComponent(deviceId)}`)
      devices.applyLink(deviceId, null)
    } finally {
      saving.value = false
    }
  }

  return { saving, save, remove }
})
