/** Roku ECP key names; must match SixBench.Common.Enums.RokuKey. */
export const ROKU_KEYS = [
  'Home',
  'Rev',
  'Fwd',
  'Play',
  'Select',
  'Left',
  'Right',
  'Down',
  'Up',
  'Back',
  'InstantReplay',
  'Info',
  'Backspace',
  'Search',
  'Enter',
  'VolumeUp',
  'VolumeDown',
  'VolumeMute',
  'PowerOn',
  'PowerOff',
  'Power',
] as const

export type RokuKey = (typeof ROKU_KEYS)[number]

export type KeyAction = 'Press' | 'Down' | 'Up'

/** Body for POST /roku-devices/{id}/keys. */
export interface KeyCommandRequest {
  key: RokuKey
  action?: KeyAction
}

/** Body for POST /roku-devices/{id}/text. */
export interface TextInputRequest {
  text: string
}
