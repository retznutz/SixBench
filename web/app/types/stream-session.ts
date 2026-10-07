/** Per-client stats reported to the server (GET /streams). */
export interface ClientStats {
  clientId: string
  decoder: string | null
  fps: number | null
  jitterMs: number | null
  droppedFrames: number | null
  serverDroppedFrames: number
  reportedUtc: string | null
}

/** An active capture session (GET /streams). */
export interface StreamSession {
  id: string
  stableId: string
  startedUtc: string
  clientCount: number
  audioClientCount: number
  framesOut: number
  bytesOut: number
  audioRunning: boolean
  clients: ClientStats[]
}
