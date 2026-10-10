import { FetchError } from 'ofetch'
import { ApiError, type ProblemDetails } from '~/types/api-error'

interface RequestOptions {
  query?: Record<string, unknown>
  /** JSON-serialisable object, or `FormData` for file uploads. */
  body?: unknown
  /** Suppress the error toast (the caller shows its own message). */
  silent?: boolean
}

/** Converts any fetch failure into an {@link ApiError}. */
export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error
  if (error instanceof FetchError) {
    const problem = (error.data ?? {}) as ProblemDetails
    const status = error.statusCode ?? problem.status ?? 0
    if (status === 0) return new ApiError(0, 'Server unreachable', 'Could not reach the SixBench server.')
    return new ApiError(
      status,
      problem.title ?? error.statusMessage ?? 'Request failed',
      problem.detail,
      problem.errors,
    )
  }
  return new ApiError(0, 'Unexpected error', (error as Error)?.message)
}

/** A file returned by the API. */
export interface DownloadedFile {
  blob: Blob
  /** From `Content-Disposition`, or the fallback passed to `download`. */
  fileName: string
}

/** Reads the file name from a `Content-Disposition` header (RFC 6266, including `filename*`). */
function fileNameFrom(disposition: string | null): string | null {
  if (!disposition) return null
  const extended = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(disposition)
  if (extended?.[1]) {
    try {
      return decodeURIComponent(extended[1].trim().replace(/^"|"$/g, ''))
    } catch {
      // Fall back to the plain filename.
    }
  }
  return /filename="?([^";]+)"?/i.exec(disposition)?.[1]?.trim() ?? null
}

/**
 * Typed access to the SixBench API. Errors become {@link ApiError}s and, unless `silent`, a toast.
 */
export function useApi() {
  const config = useRuntimeConfig()
  const toast = useToast()
  const router = useRouter()

  async function request<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', path: string, options: RequestOptions = {}) {
    try {
      return await $fetch<T>(path, {
        baseURL: config.public.apiBase,
        method,
        query: options.query,
        body: options.body as Record<string, unknown> | FormData | undefined,
      })
    } catch (raw) {
      throw await fail(raw, path, options)
    }
  }

  /** POSTs and returns the response body as a file (screenshots, packages). */
  async function download(path: string, fallbackName: string, options: RequestOptions = {}): Promise<DownloadedFile> {
    try {
      const response = await $fetch.raw<Blob>(path, {
        baseURL: config.public.apiBase,
        method: 'POST',
        query: options.query,
        body: options.body as Record<string, unknown> | FormData | undefined,
        responseType: 'blob',
      })
      return {
        blob: response._data ?? new Blob(),
        fileName: fileNameFrom(response.headers.get('content-disposition')) ?? fallbackName,
      }
    } catch (raw) {
      // With a blob response type, error bodies arrive as Blobs too; read the ProblemDetails out of them.
      if (raw instanceof FetchError && raw.data instanceof Blob) {
        try {
          raw.data = JSON.parse(await raw.data.text())
        } catch {
          raw.data = undefined
        }
      }
      throw await fail(raw, path, options)
    }
  }

  /** Normalises a failure, handles an ended session and shows the toast. */
  async function fail(raw: unknown, path: string, options: RequestOptions): Promise<ApiError> {
    const error = toApiError(raw)

    // The session ended (expired, signed out elsewhere, or the account was removed): go to the login page.
    if (error.status === 401 && !path.startsWith('/auth/')) {
      useAuthStore().clear()
      const current = router.currentRoute.value
      if (current.path !== '/login') {
        await router.push({ path: '/login', query: { redirect: current.fullPath } })
      }
      return error
    }

    if (!options.silent) {
      toast.add({ severity: 'error', summary: error.title, detail: error.userMessage, life: 6000 })
    }
    return error
  }

  return {
    get: <T>(path: string, options?: RequestOptions) => request<T>('GET', path, options),
    post: <T>(path: string, options?: RequestOptions) => request<T>('POST', path, options),
    put: <T>(path: string, options?: RequestOptions) => request<T>('PUT', path, options),
    del: <T = void>(path: string, options?: RequestOptions) => request<T>('DELETE', path, options),
    download,
  }
}
