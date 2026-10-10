import { defineStore } from 'pinia'
import type { ApiError } from '~/types/api-error'
import type { PackageChannelRequest, RokuDevActionResult, RokuDevChannelStatus } from '~/types/roku-dev-channel'
import { downloadBlob } from '~/lib/stream/snapshot'

/** Developer web server actions, for busy and result tracking. */
export type DevChannelAction =
  'install' | 'delete' | 'launch' | 'squashfs' | 'screenshot' | 'package' | 'rekey' | 'reboot' | 'check-update'

/**
 * The Roku developer web page, done through the server: sideloading, packaging and utilities.
 * Requests are silent; the last result or error is kept in state and shown inline, because the Roku's own
 * wording ("Install Failure: Compilation Failed") is what a developer needs to see.
 */
export const useRokuDevChannelStore = defineStore('rokuDevChannel', () => {
  const api = useApi()

  const status = ref<RokuDevChannelStatus | null>(null)
  const statusLoading = ref(false)
  const statusError = ref<ApiError | null>(null)

  const busy = ref<DevChannelAction | null>(null)
  const lastAction = ref<DevChannelAction | null>(null)
  const lastResult = ref<RokuDevActionResult | null>(null)
  const lastError = ref<ApiError | null>(null)

  /** Object URL of the latest developer screenshot, for the preview. */
  const screenshotUrl = ref<string | null>(null)
  const screenshotName = ref('')
  let screenshotBlob: Blob | null = null

  const base = (rokuId: number) => `/roku-devices/${rokuId}/dev-channel`

  async function fetchStatus(rokuId: number) {
    statusLoading.value = true
    statusError.value = null
    try {
      status.value = await api.get<RokuDevChannelStatus>(base(rokuId), { silent: true })
    } catch (e) {
      statusError.value = e as ApiError
    } finally {
      statusLoading.value = false
    }
  }

  /** Runs one action, tracking busy state and keeping its outcome for display. */
  async function run<T>(action: DevChannelAction, call: () => Promise<T>): Promise<T | undefined> {
    busy.value = action
    lastAction.value = action
    lastResult.value = null
    lastError.value = null
    try {
      return await call()
    } catch (e) {
      lastError.value = e as ApiError
      return undefined
    } finally {
      busy.value = null
    }
  }

  async function runAndRefresh(rokuId: number, action: DevChannelAction, call: () => Promise<RokuDevActionResult>) {
    const result = await run(action, call)
    if (result) {
      lastResult.value = result
      await fetchStatus(rokuId)
    }
    return result
  }

  function install(rokuId: number, archive: File) {
    const body = new FormData()
    body.append('archive', archive, archive.name)
    return runAndRefresh(rokuId, 'install', () => api.post<RokuDevActionResult>(base(rokuId), { body, silent: true }))
  }

  function remove(rokuId: number) {
    return runAndRefresh(rokuId, 'delete', () => api.del<RokuDevActionResult>(base(rokuId), { silent: true }))
  }

  async function launch(rokuId: number) {
    await run('launch', async () => {
      await api.post<unknown>(`${base(rokuId)}/launch`, { silent: true })
      lastResult.value = { summary: 'Launched the sideloaded channel.', messages: [] }
    })
  }

  function convertToSquashfs(rokuId: number) {
    return runAndRefresh(rokuId, 'squashfs', () =>
      api.post<RokuDevActionResult>(`${base(rokuId)}/squashfs`, { silent: true }),
    )
  }

  async function screenshot(rokuId: number) {
    const file = await run('screenshot', () =>
      api.download(`${base(rokuId)}/screenshot`, 'roku-dev.jpg', { silent: true }),
    )
    if (!file) return
    if (screenshotUrl.value) URL.revokeObjectURL(screenshotUrl.value)
    screenshotBlob = file.blob
    screenshotName.value = file.fileName
    screenshotUrl.value = URL.createObjectURL(file.blob)
  }

  function saveScreenshot() {
    if (screenshotBlob) downloadBlob(screenshotBlob, screenshotName.value)
  }

  async function packageChannel(rokuId: number, request: PackageChannelRequest) {
    const file = await run('package', () =>
      api.download(`${base(rokuId)}/package`, `${request.appName}_${request.version}.pkg`, {
        body: request,
        silent: true,
      }),
    )
    if (!file) return false
    downloadBlob(file.blob, file.fileName)
    lastResult.value = { summary: `Signed package ${file.fileName} downloaded.`, messages: [] }
    return true
  }

  function rekey(rokuId: number, pkg: File, signingPassword: string) {
    const body = new FormData()
    body.append('package', pkg, pkg.name)
    body.append('signingPassword', signingPassword)
    return runAndRefresh(rokuId, 'rekey', () =>
      api.post<RokuDevActionResult>(`${base(rokuId)}/rekey`, { body, silent: true }),
    )
  }

  function reboot(rokuId: number) {
    return runAndRefresh(rokuId, 'reboot', () =>
      api.post<RokuDevActionResult>(`${base(rokuId)}/reboot`, { silent: true }),
    )
  }

  function checkForUpdate(rokuId: number) {
    return runAndRefresh(rokuId, 'check-update', () =>
      api.post<RokuDevActionResult>(`${base(rokuId)}/check-update`, { silent: true }),
    )
  }

  /** Forgets everything (e.g. when switching to another Roku). */
  function reset() {
    status.value = null
    statusError.value = null
    lastAction.value = null
    lastResult.value = null
    lastError.value = null
    if (screenshotUrl.value) URL.revokeObjectURL(screenshotUrl.value)
    screenshotUrl.value = null
    screenshotBlob = null
  }

  return {
    status,
    statusLoading,
    statusError,
    busy,
    lastAction,
    lastResult,
    lastError,
    screenshotUrl,
    fetchStatus,
    install,
    remove,
    launch,
    convertToSquashfs,
    screenshot,
    saveScreenshot,
    packageChannel,
    rekey,
    reboot,
    checkForUpdate,
    reset,
  }
})
