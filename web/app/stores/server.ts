import { defineStore } from 'pinia'

export const useServerStore = defineStore('server', () => {
  const api = useApi()
  const restarting = ref(false)

  /** Restarts the server. Returns whether it started a new process itself (otherwise a service manager must). */
  async function restart() {
    restarting.value = true
    try {
      return (await api.post<{ relaunched: boolean; message: string }>('/server/restart')).relaunched
    } catch (error) {
      restarting.value = false
      throw error
    }
  }

  return { restarting, restart }
})
