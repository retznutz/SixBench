import type { MaybeRefOrGetter } from 'vue'
import type { SgHighlight, SgHighlightMessage } from '~/types/roku-dev-tools'

/** One channel per Roku, shared by its pop-out developer tools window and every watch page showing it. */
const channelName = (rokuId: number) => `sixbench-sg-highlight-${rokuId}`

/**
 * Pop-out side: sends the node to outline to watch pages for the same Roku, which are in another window.
 * Sends null when the window closes so the outline doesn't linger.
 * @param rokuId The Roku being inspected.
 * @param highlight The node to outline, or null.
 */
export function useSgHighlightPublisher(
  rokuId: MaybeRefOrGetter<number>,
  highlight: MaybeRefOrGetter<SgHighlight | null>,
) {
  let channel: BroadcastChannel | null = null

  function send() {
    channel?.postMessage({ kind: 'highlight', highlight: toValue(highlight) } satisfies SgHighlightMessage)
  }

  function close() {
    if (!channel) return
    channel.postMessage({ kind: 'highlight', highlight: null } satisfies SgHighlightMessage)
    channel.close()
    channel = null
  }

  function open(id: number) {
    close()
    if (typeof BroadcastChannel === 'undefined') return
    channel = new BroadcastChannel(channelName(id))
    channel.onmessage = (e: MessageEvent<SgHighlightMessage>) => {
      if (e.data?.kind === 'request') send()
    }
    send()
  }

  watch(() => toValue(rokuId), open, { immediate: true })
  watch(() => toValue(highlight), send)

  // Closing the window doesn't always unmount components; pagehide does fire.
  window.addEventListener('pagehide', close)
  onBeforeUnmount(() => {
    window.removeEventListener('pagehide', close)
    close()
  })
}

/**
 * Watch side: the node a pop-out developer tools window for this Roku wants outlined.
 * @param rokuId The Roku linked to the video, or null when there is none.
 * @returns The node to outline, or null.
 */
export function useSgHighlightSubscription(rokuId: MaybeRefOrGetter<number | null>) {
  const highlight = shallowRef<SgHighlight | null>(null)
  let channel: BroadcastChannel | null = null

  function close() {
    channel?.close()
    channel = null
  }

  function open(id: number | null) {
    close()
    highlight.value = null
    if (id == null || typeof BroadcastChannel === 'undefined') return
    channel = new BroadcastChannel(channelName(id))
    channel.onmessage = (e: MessageEvent<SgHighlightMessage>) => {
      if (e.data?.kind === 'highlight') highlight.value = e.data.highlight
    }
    // A pop-out may already be open (e.g. this page was reloaded): ask for its current highlight.
    channel.postMessage({ kind: 'request' } satisfies SgHighlightMessage)
  }

  watch(() => toValue(rokuId), open, { immediate: true })
  onBeforeUnmount(close)

  return highlight
}
