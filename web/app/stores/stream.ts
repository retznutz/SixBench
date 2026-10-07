import { defineStore } from 'pinia'
import { DeviceAudioPlayer } from '~/lib/stream/DeviceAudioPlayer'
import { MseRenderer } from '~/lib/stream/MseRenderer'
import { StreamClient } from '~/lib/stream/StreamClient'
import { WebCodecsRenderer } from '~/lib/stream/WebCodecsRenderer'
import type { VideoRenderer } from '~/lib/stream/VideoRenderer'
import type { AudioGrant, DecoderKind, PlaybackStats, StreamStatus } from '~/types/stream-protocol'

export type AudioStatus = 'off' | 'starting' | 'on' | 'unavailable' | 'not_permitted'

/** Elements the active renderer draws into. */
export interface StreamSurface {
  canvas: HTMLCanvasElement | null
  video: HTMLVideoElement | null
}

/**
 * State of the live stream on the watch page. The WebSocket client, renderer and audio player are
 * plain objects held outside reactive state; components drive them through these actions.
 */
export const useStreamStore = defineStore('stream', () => {
  const config = useRuntimeConfig()

  const deviceId = ref<string | null>(null)
  const status = ref<StreamStatus>('idle')
  const message = ref<string | null>(null)
  const decoder = ref<DecoderKind>('webcodecs')
  const sessionId = ref<string | null>(null)
  const grant = ref<AudioGrant>({ device: false, mic: false })
  const audioStatus = ref<AudioStatus>('off')
  const audioMessage = ref<string | null>(null)
  const outputDeviceId = ref('default')
  const stats = ref<PlaybackStats | null>(null)

  let client: StreamClient | null = null
  let renderer: VideoRenderer | null = null
  let audioPlayer: DeviceAudioPlayer | null = null

  const audioSupported = computed(() => import.meta.client && DeviceAudioPlayer.isSupported())

  function socketUrl(id: string): string {
    const scheme = location.protocol === 'https:' ? 'wss' : 'ws'
    const origin = (config.public.streamOrigin as string) || `${scheme}://${location.host}`
    return `${origin}${config.public.apiBase}/streams/${encodeURIComponent(id)}/ws`
  }

  function connect(id: string, kind: DecoderKind, surface: StreamSurface) {
    disconnect()
    deviceId.value = id
    decoder.value = kind
    message.value = null
    stats.value = null

    const callbacks = {
      onNeedKeyframe: () => client?.requestKeyframe(),
      onError: (msg: string) => {
        status.value = 'error'
        message.value = msg
      },
    }

    if (kind === 'mse') {
      if (!MseRenderer.isSupported() || !surface.video) {
        status.value = 'unsupported'
        message.value = 'This browser does not support Media Source Extensions.'
        return
      }
      renderer = new MseRenderer(surface.video, callbacks)
    } else {
      if (!WebCodecsRenderer.isSupported() || !surface.canvas) {
        status.value = 'unsupported'
        message.value = 'Please use Chrome/Edge 94+ or Safari 16.4+.'
        return
      }
      renderer = new WebCodecsRenderer(surface.canvas, callbacks)
    }

    client = new StreamClient(socketUrl(id), renderer, {
      onStatus: (s, msg) => {
        status.value = s
        message.value = msg ?? null
      },
      onHello: (hello) => {
        sessionId.value = hello.session_id
        grant.value = hello.audio_grant
        if (!hello.audio_grant.device && audioStatus.value === 'on') void stopAudio()
      },
      onAudioState: (enabled, error, msg) => {
        if (enabled) {
          audioStatus.value = 'on'
          audioMessage.value = null
        } else if (error) {
          audioStatus.value = 'unavailable'
          audioMessage.value = msg ?? 'Device audio stopped.'
          void releaseAudioPlayer()
        }
      },
      onServerError: (code, msg) => {
        if (code === 'not_permitted' && audioStatus.value === 'starting') {
          audioStatus.value = 'not_permitted'
          audioMessage.value = msg
          void releaseAudioPlayer()
        } else if (code === 'audio_unavailable') {
          audioStatus.value = 'unavailable'
          audioMessage.value = msg
          void releaseAudioPlayer()
        }
      },
      onStats: (s) => {
        stats.value = s
      },
    })
    client.connect()
  }

  function disconnect() {
    void releaseAudioPlayer()
    client?.close()
    renderer?.close()
    client = null
    renderer = null
    status.value = 'idle'
    sessionId.value = null
    grant.value = { device: false, mic: false }
    audioStatus.value = 'off'
    audioMessage.value = null
  }

  function requestKeyframe() {
    client?.requestKeyframe()
  }

  /** Turns device audio on. Call from a click handler so the AudioContext may start. */
  async function startAudio() {
    if (!client) return
    if (!audioSupported.value) {
      audioStatus.value = 'unavailable'
      audioMessage.value = 'audio:unavailable — this browser lacks WebCodecs audio. Video continues without sound.'
      return
    }
    if (!grant.value.device) {
      audioStatus.value = 'not_permitted'
      audioMessage.value = 'Device audio is not enabled for this encoder.'
      return
    }
    audioStatus.value = 'starting'
    audioMessage.value = null
    const player = new DeviceAudioPlayer((msg) => {
      audioStatus.value = 'unavailable'
      audioMessage.value = msg
    })
    try {
      await player.start(outputDeviceId.value)
    } catch {
      audioStatus.value = 'unavailable'
      audioMessage.value = 'audio:unavailable — Opus decoding is not supported here. Video continues without sound.'
      return
    }
    audioPlayer = player
    client.setAudio(player)
  }

  async function stopAudio() {
    client?.setAudio(null)
    await releaseAudioPlayer()
    audioStatus.value = 'off'
  }

  async function setOutputDevice(id: string) {
    outputDeviceId.value = id
    await audioPlayer?.setOutputDevice(id)
  }

  async function releaseAudioPlayer() {
    const player = audioPlayer
    audioPlayer = null
    await player?.stop()
  }

  return {
    deviceId,
    status,
    message,
    decoder,
    sessionId,
    grant,
    audioStatus,
    audioMessage,
    audioSupported,
    outputDeviceId,
    stats,
    connect,
    disconnect,
    requestKeyframe,
    startAudio,
    stopAudio,
    setOutputDevice,
  }
})
