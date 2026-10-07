import { FetchError } from 'ofetch'
import { ApiError, type ProblemDetails } from '~/types/api-error'

interface RequestOptions {
  query?: Record<string, unknown>
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
        body: options.body as Record<string, unknown> | undefined,
      })
    } catch (raw) {
      const error = toApiError(raw)

      // The session ended (expired, signed out elsewhere, or the account was removed): go to the login page.
      if (error.status === 401 && !path.startsWith('/auth/')) {
        useAuthStore().clear()
        const current = router.currentRoute.value
        if (current.path !== '/login') {
          await router.push({ path: '/login', query: { redirect: current.fullPath } })
        }
        throw error
      }

      if (!options.silent) {
        toast.add({ severity: 'error', summary: error.title, detail: error.userMessage, life: 6000 })
      }
      throw error
    }
  }

  return {
    get: <T>(path: string, options?: RequestOptions) => request<T>('GET', path, options),
    post: <T>(path: string, options?: RequestOptions) => request<T>('POST', path, options),
    put: <T>(path: string, options?: RequestOptions) => request<T>('PUT', path, options),
    del: <T = void>(path: string, options?: RequestOptions) => request<T>('DELETE', path, options),
  }
}
