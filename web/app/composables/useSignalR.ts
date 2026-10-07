import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'

/**
 * A SignalR hub connection with automatic reconnect. Register handlers with `build().on(...)` before `start()`.
 * @param hubPath Hub path on this origin, e.g. `/hubs/certificates`.
 */
export function useSignalR(hubPath: string) {
  const connection = shallowRef<HubConnection | null>(null)
  const connectionState = ref<HubConnectionState>(HubConnectionState.Disconnected)

  /** Creates the connection and wires lifecycle events, but does not start it. */
  function build(): HubConnection {
    if (connection.value) return connection.value

    const conn = new HubConnectionBuilder()
      .withUrl(hubPath, { withCredentials: true })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build()
    conn.onreconnecting(() => (connectionState.value = HubConnectionState.Reconnecting))
    conn.onreconnected(() => (connectionState.value = HubConnectionState.Connected))
    conn.onclose(() => (connectionState.value = HubConnectionState.Disconnected))
    connection.value = conn
    return conn
  }

  /** Builds (if needed) and starts the connection. Failures leave it disconnected. */
  async function start() {
    const conn = build()
    if (conn.state === HubConnectionState.Connected) return
    try {
      await conn.start()
      connectionState.value = HubConnectionState.Connected
    } catch {
      connectionState.value = HubConnectionState.Disconnected
    }
  }

  async function stop() {
    if (connection.value) {
      await connection.value.stop()
      connection.value = null
      connectionState.value = HubConnectionState.Disconnected
    }
  }

  return { connection: readonly(connection), connectionState: readonly(connectionState), build, start, stop }
}
