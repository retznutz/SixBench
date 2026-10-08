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

export interface KeyboardShortcutHandlers {
  onKey: (key: RokuKey) => void
  /** "T" opens the text-entry dialog. */
  onTextEntry: () => void
  /** "S" saves a screenshot. */
  onScreenshot?: () => void
}

/**
 * Maps keyboard input on `target` to Roku keys. Modifier combos are ignored so browser shortcuts keep working.
 */
export function useKeyboardShortcuts(target: Ref<HTMLElement | null>, handlers: KeyboardShortcutHandlers) {
  function onKeydown(event: KeyboardEvent) {
    if (event.ctrlKey || event.metaKey || event.altKey) return

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
