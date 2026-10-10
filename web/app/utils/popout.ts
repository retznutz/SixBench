/** Size of a newly opened developer tools window. */
const DEV_TOOLS_WINDOW_FEATURES = 'popup=yes,width=1100,height=850'

/**
 * Opens the developer tools for a Roku in their own browser window, or brings that window forward if it is already
 * open. Call it straight from a click handler (no `await` first) or the browser will block it.
 * @param rokuId The Roku to inspect.
 * @param tab The tab to start on.
 * @returns False if the browser blocked the window.
 */
export function openDevToolsWindow(rokuId: number, tab?: string): boolean {
  const url = `/devtools/${rokuId}${tab ? `?tab=${encodeURIComponent(tab)}` : ''}`
  // One window per Roku: opening by name reuses it instead of stacking duplicates.
  const win = window.open('', `sixbench-devtools-${rokuId}`, DEV_TOOLS_WINDOW_FEATURES)
  if (!win) return false

  let isNew: boolean
  try {
    isNew = win.location.href === 'about:blank'
  } catch {
    // The window has navigated to another site; take it back.
    isNew = true
  }

  // An existing window keeps its state and is only brought forward.
  if (isNew) win.location.replace(url)
  win.focus()
  return true
}
