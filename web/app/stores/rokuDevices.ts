import { defineStore } from 'pinia'
import type { AddRokuRequest, RokuDevice } from '~/types/roku-device'

export const useRokuDevicesStore = defineStore('rokuDevices', () => {
  const api = useApi()

  const devices = ref<RokuDevice[]>([])
  const loading = ref(false)
  const discovering = ref(false)
  const adding = ref(false)

  function byId(id: number | null | undefined): RokuDevice | undefined {
    return id == null ? undefined : devices.value.find((d) => d.id === id)
  }

  async function fetchAll() {
    loading.value = true
    try {
      devices.value = await api.get<RokuDevice[]>('/roku-devices')
    } finally {
      loading.value = false
    }
  }

  /** Runs SSDP discovery (a few seconds) and returns the devices found. */
  async function discover() {
    discovering.value = true
    try {
      const found = await api.post<RokuDevice[]>('/roku-devices/discover')
      await fetchAll()
      return found
    } finally {
      discovering.value = false
    }
  }

  async function addManual(request: AddRokuRequest) {
    adding.value = true
    try {
      const device = await api.post<RokuDevice>('/roku-devices', { body: request })
      await fetchAll()
      return device
    } finally {
      adding.value = false
    }
  }

  async function remove(id: number) {
    await api.del(`/roku-devices/${id}`)
    devices.value = devices.value.filter((d) => d.id !== id)
  }

  return { devices, loading, discovering, adding, byId, fetchAll, discover, addManual, remove }
})
