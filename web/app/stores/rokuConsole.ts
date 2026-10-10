import { defineStore } from 'pinia'
import { HubConnectionState } from '@microsoft/signalr'
import type { RokuConsoleOutput, RokuConsoleSnapshot, RokuConsoleStatus } from '~/types/roku-dev-channel'

/** How much console text to keep in the browser. */
export const CONSOLE_MAX_CHARS = 200_000

/**
 * The BrightScript debug console of one Roku, streamed over SignalR (/hubs/roku-console). The server holds the
 * single telnet connection the Roku allows and shares it with every viewer.
 */
export const useRokuConsoleStore = defineStore('rokuConsole', () => {
  const hub = useSignalR('/hubs/roku-console')

  const rokuId = ref<number | null>(null)
  const text = ref('')
  const status = ref<RokuConsoleStatus | null>(null)
  const error = ref<string | null>(null)
  const connected = computed(() => hub.connectionState.value === HubConnectionState.Connected)

  let lastSequence = 0
  let wired = false
  /** Output that arrives while a subscribe call is in flight; applied once the snapshot is in. */
  let pending: RokuConsoleOutput[] | null = null

  function append(chunk: string) {
    const next = text.value + chunk
    text.value = next.length > CONSOLE_MAX_CHARS ? next.slice(next.length - CONSOLE_MAX_CHARS) : next
  }

  function receive(output: RokuConsoleOutput) {
    if (output.rokuId !== rokuId.value) return
    if (pending) {
      pending.push(output)
      return
    }
    if (output.sequence <= lastSequence) return
    lastSequence = output.sequence
    append(output.text)
  }

  function wire() {
    const connection = hub.build()
    if (wired) return connection
    wired = true
    connection.on('ConsoleOutput', receive)
    connection.on('ConsoleStatus', (next: RokuConsoleStatus) => {
      if (next.rokuId === rokuId.value) status.value = next
    })
    // Group membership does not survive a reconnect; subscribe again (the snapshot fills any gap).
    connection.onreconnected(() => {
      if (rokuId.value != null) void subscribe(rokuId.value)
    })
    return connection
  }

  async function subscribe(id: number) {
    const connection = wire()
    pending = []
    try {
      const snapshot = await connection.invoke<RokuConsoleSnapshot>('Subscribe', id)
      if (id !== rokuId.value) return
      text.value = snapshot.backlog
      lastSequence = snapshot.sequence
      status.value = snapshot.status
      error.value = null
    } catch (e) {
      error.value = (e as Error).message
    } finally {
      const buffered = pending ?? []
      pending = null
      buffered.forEach(receive)
    }
  }

  /** Starts showing a Roku's console (stops any other). */
  async function watch(id: number) {
    if (rokuId.value === id && connected.value) return
    await unwatch()
    rokuId.value = id
    text.value = ''
    lastSequence = 0
    error.value = null
    wire()
    await hub.start()
    if (!connected.value) {
      error.value = 'Could not connect to the SixBench server for the debug console.'
      return
    }
    await subscribe(id)
  }

  /** Stops showing the console; the server closes the Roku connection when nobody is watching. */
  async function unwatch() {
    const id = rokuId.value
    rokuId.value = null
    status.value = null
    if (id != null && connected.value) {
      try {
        await wire().invoke('Unsubscribe', id)
      } catch {
        // The server drops subscriptions when the connection closes anyway.
      }
    }
  }

  /** Types a line into the console, e.g. `bt` or `cont` at a debugger prompt. */
  async function send(line: string) {
    if (rokuId.value == null || !connected.value) return false
    try {
      await wire().invoke('Send', rokuId.value, line)
      error.value = null
      return true
    } catch (e) {
      error.value = (e as Error).message
      return false
    }
  }

  /** Clears this browser's view only; other viewers keep their output. */
  function clear() {
    text.value = ''
  }

  return { rokuId, text, status, error, connected, watch, unwatch, send, clear }
})
