import type { Ref } from 'vue'
import type { RokuKey } from '~/types/remote'

/** Keyboard → Roku key map, active while the video surface has focus. */
export const KEYBOARD_SHORTCUTS: ReadonlyArray<{ keys: string[]; label: string; roku: RokuKey }> = [
  { keys: ['ArrowUp'], label: '↑', roku: 'Up' },
  { keys: ['ArrowDown'], label: '↓', roku: 'Down' },
  { keys: ['ArrowLeft'], label: '←', roku: 'Left' },
  { keys: ['ArrowRight'], label: '→', roku: 'Right' },
  { keys: ['Enter'], label: 'Enter', roku: 'Select' },
  { keys: ['Backspace', 'Escape'], label: 'Backspace / Esc', roku: 'Back' },
  { keys: ['h', 'H'], label: 'H', roku: 'Home' },
  { keys: [' '], label: 'Space', roku: 'Play' },
  { keys: [','], label: ',', roku: 'Rev' },
  { keys: ['.'], label: '.', roku: 'Fwd' },
  { keys: ['r', 'R'], label: 'R', roku: 'InstantReplay' },
  { keys: ['i', 'I'], label: 'I', roku: 'Info' },
  { keys: ['+', '='], label: '+', roku: 'VolumeUp' },
  { keys: ['-', '_'], label: '−', roku: 'VolumeDown' },
]

/** Keys whose auto-repeat should keep sending (navigation, scrubbing, volume). */
const REPEATABLE: ReadonlySet<RokuKey> = new Set([
  'Up',
  'Down',
  'Left',
  'Right',
  'Rev',
  'Fwd',
  'VolumeUp',
  'VolumeDown',
])

const lookup = new Map<string, RokuKey>(KEYBOARD_SHORTCUTS.flatMap((s) => s.keys.map((k) => [k, s.roku] as const)))

/**
 * Keys that still act on the Roku while typing. Everything else that prints a character (Space, letters, `+`, `,` …)
 * is typed instead of triggering its shortcut.
 */
const TYPING_KEYS: ReadonlyMap<string, RokuKey> = new Map([
  ['ArrowUp', 'Up'],
  ['ArrowDown', 'Down'],
  ['ArrowLeft', 'Left'],
  ['ArrowRight', 'Right'],
  ['Backspace', 'Backspace'],
  ['Enter', 'Enter'],
  ['Escape', 'Back'],
])

export interface KeyboardShortcutHandlers {
  onKey: (key: RokuKey) => void
  /** "T" opens the text-entry dialog. */
  onTextEntry: () => void
  /** "S" saves a screenshot. */
  onScreenshot?: () => void
  /** While true, keys are typed on the Roku with `onText` instead of acting as shortcuts. */
  typing?: Ref<boolean>
  /** Types one character on the Roku. */
  onText?: (char: string) => void
}

/**
 * Maps keyboard input on `target` to Roku keys. Modifier combos are ignored so browser shortcuts keep working.
 */
export function useKeyboardShortcuts(target: Ref<HTMLElement | null>, handlers: KeyboardShortcutHandlers) {
  /** Typing mode: printable keys become text; arrows, Backspace, Enter and Esc still drive the Roku. */
  function onTypingKeydown(event: KeyboardEvent) {
    if (event.isComposing) return
    const roku = TYPING_KEYS.get(event.key)
    if (roku) {
      event.preventDefault()
      if (event.repeat && roku !== 'Backspace' && !REPEATABLE.has(roku)) return
      handlers.onKey(roku)
    } else if (event.key.length === 1) {
      event.preventDefault()
      // A held key would queue a request per repeat; type it once.
      if (!event.repeat) handlers.onText?.(event.key)
    }
  }

  function onKeydown(event: KeyboardEvent) {
    if (event.ctrlKey || event.metaKey || event.altKey) return

    if (handlers.typing?.value && handlers.onText) {
      onTypingKeydown(event)
      return
    }

    if (event.key === 't' || event.key === 'T') {
      event.preventDefault()
      if (!event.repeat) handlers.onTextEntry()
      return
    }

    if ((event.key === 's' || event.key === 'S') && handlers.onScreenshot) {
      event.preventDefault()
      if (!event.repeat) handlers.onScreenshot()
      return
    }

    const roku = lookup.get(event.key)
    if (!roku) return
    event.preventDefault()
    if (event.repeat && !REPEATABLE.has(roku)) return
    handlers.onKey(roku)
  }

  watch(
    target,
    (el, previous) => {
      previous?.removeEventListener('keydown', onKeydown)
      el?.addEventListener('keydown', onKeydown)
    },
    { immediate: true },
  )

  onBeforeUnmount(() => target.value?.removeEventListener('keydown', onKeydown))
}
