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

/** `TextEditState` event from /hubs/roku-remote: whether the Roku's on-screen keyboard is open. */
export interface RokuTextEditState {
  rokuId: number
  /** The server can see the Roku's keyboard state; while false, `open` is false. */
  available: boolean
  /** A text field on the Roku is selected and being edited. */
  open: boolean
  /** The Roku's id for that field; a new id means a different field. */
  textEditId: string | null
  /** What the field contains, when the Roku reports it. */
  text: string | null
}
