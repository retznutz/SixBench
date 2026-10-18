import { defineStore } from 'pinia'
import { watch as watchRef } from 'vue'
import { HubConnectionState } from '@microsoft/signalr'
import type { RokuTextEditState } from '~/types/remote'

const AUTO_OPEN_KEY = 'sixbench.autoOpenText'

/** Remembers whether the text box opens by itself; storage may be unavailable. */
function readAutoOpen() {
  try {
    return localStorage.getItem(AUTO_OPEN_KEY) !== '0'
  } catch {
    return true
  }
}

/**
 * Whether the watched Roku's on-screen keyboard is open, streamed over SignalR (/hubs/roku-remote). The server
 * holds one connection to each Roku and shares it with every viewer.
 */
export const useRokuTextEditStore = defineStore('rokuTextEdit', () => {
  const hub = useSignalR('/hubs/roku-remote')

  const rokuId = ref<number | null>(null)
  const state = ref<RokuTextEditState | null>(null)
  const connected = computed(() => hub.connectionState.value === HubConnectionState.Connected)
  /** A text field on the watched Roku is being edited. */
  const open = computed(() => state.value?.open === true)
  /** Open the text box when a text field is selected on the Roku. */
  const autoOpen = ref(readAutoOpen())

  // `watch` below is this store's action, so Vue's is imported under another name.
  watchRef(autoOpen, (on) => {
    try {
      localStorage.setItem(AUTO_OPEN_KEY, on ? '1' : '0')
    } catch {
      // Not persisted; the toggle still works for this visit.
    }
  })

  let wired = false

  function wire() {
    const connection = hub.build()
    if (wired) return connection
    wired = true
    connection.on('TextEditState', (next: RokuTextEditState) => {
      if (next.rokuId === rokuId.value) state.value = next
    })
    // Group membership does not survive a reconnect; subscribe again.
    connection.onreconnected(() => {
      if (rokuId.value != null) void subscribe(rokuId.value)
    })
    return connection
  }

  async function subscribe(id: number) {
    try {
      const snapshot = await wire().invoke<RokuTextEditState>('Subscribe', id)
      if (id !== rokuId.value) {
        // Unwatched while subscribing; don't leave the server connected to the Roku for nobody.
        void wire()
          .invoke('Unsubscribe', id)
          .catch(() => {})
        return
      }
      state.value = snapshot
    } catch {
      // Not essential: the text box just won't open by itself.
      state.value = null
    }
  }

  /** Starts following a Roku's keyboard (stops any other). */
  async function watch(id: number) {
    if (rokuId.value === id && connected.value) return
    await unwatch()
    rokuId.value = id
    wire()
    await hub.start()
    if (connected.value) await subscribe(id)
  }

  /** Stops following; the server disconnects from the Roku when nobody is watching. */
  async function unwatch() {
    const id = rokuId.value
    rokuId.value = null
    state.value = null
    if (id != null && connected.value) {
      try {
        await wire().invoke('Unsubscribe', id)
      } catch {
        // The server drops subscriptions when the connection closes anyway.
      }
    }
  }

  return { rokuId, state, open, autoOpen, connected, watch, unwatch }
})
