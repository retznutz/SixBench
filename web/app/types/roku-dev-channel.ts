/** A banner the Roku developer web server showed for an action. */
export interface RokuDevMessage {
  type: 'success' | 'info' | 'error'
  text: string
}

/** Outcome of a developer web server action that succeeded. */
export interface RokuDevActionResult {
  summary: string
  messages: RokuDevMessage[]
}

/** A channel installed on the Roku. */
export interface RokuApp {
  id: string
  name: string
  version: string | null
}

/** Response of GET /roku-devices/{id}/dev-channel. */
export interface RokuDevChannelStatus {
  /** Null when the Roku did not report it. */
  developerModeEnabled: boolean | null
  /** Developer id of the Roku's signing key; null when it has none (packaging needs one). */
  keyedDeveloperId: string | null
  hasDevPassword: boolean
  /** The sideloaded channel, if installed. */
  devChannel: RokuApp | null
}

/** Body for PUT /roku-devices/{id}/dev-password. */
export interface SetDevPasswordRequest {
  password: string
}

/** Body for POST /roku-devices/{id}/dev-channel/package. */
export interface PackageChannelRequest {
  appName: string
  version: string
  /** Signing key password; sent for this request only. */
  signingPassword: string
}

/** Debug console connection state; must match RokuDebugConsoleManager's constants. */
export type RokuConsoleState = 'Connecting' | 'Connected' | 'Disconnected'

/** `ConsoleStatus` event from /hubs/roku-console. */
export interface RokuConsoleStatus {
  rokuId: number
  state: RokuConsoleState
  message: string | null
}

/** `ConsoleOutput` event from /hubs/roku-console. */
export interface RokuConsoleOutput {
  rokuId: number
  sequence: number
  text: string
}

/** Result of the hub's `Subscribe` method. */
export interface RokuConsoleSnapshot {
  status: RokuConsoleStatus
  backlog: string
  /** Output at or below this sequence is already in `backlog`. */
  sequence: number
}
