import { defineStore } from 'pinia'
import type { KeyAction, KeyCommandRequest, RokuKey, TextInputRequest } from '~/types/remote'

/**
 * Sends remote input. Commands are queued and sent one at a time so key order is preserved
 * even when the user presses faster than the Roku answers.
 */
export const useRemoteStore = defineStore('remote', () => {
  const api = useApi()

  const pending = ref(0)
  const lastKey = ref<RokuKey | null>(null)
  let queue: Promise<unknown> = Promise.resolve()

  function enqueue<T>(work: () => Promise<T>): Promise<T> {
    pending.value++
    const next = queue.then(work, work).finally(() => pending.value--)
    // Keep the chain alive after failures; the caller still sees the rejection.
    queue = next.catch(() => undefined)
    return next
  }

  function sendKey(rokuId: number, key: RokuKey, action: KeyAction = 'Press') {
    lastKey.value = key
    const body: KeyCommandRequest = { key, action }
    return enqueue(() => api.post<undefined>(`/roku-devices/${rokuId}/keys`, { body }))
  }

  function sendText(rokuId: number, text: string) {
    const body: TextInputRequest = { text }
    return enqueue(() => api.post<undefined>(`/roku-devices/${rokuId}/text`, { body }))
  }

  return { pending, lastKey, sendKey, sendText }
})
