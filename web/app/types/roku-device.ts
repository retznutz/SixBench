/** A saved Roku (GET /roku-devices). */
export interface RokuDevice {
  id: number
  serialNumber: string
  friendlyName: string
  model: string | null
  ipAddress: string
  port: number
  isManual: boolean
  lastSeenUtc: string
  /** True if the developer-mode password is saved on the server (it is never returned). */
  hasDevPassword: boolean
}

/** Body for POST /roku-devices. */
export interface AddRokuRequest {
  host: string
  port?: number | null
}
