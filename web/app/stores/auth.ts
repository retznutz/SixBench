import { defineStore } from 'pinia'
import type { ChangePasswordRequest, CurrentUser, LoginRequest } from '~/types/auth'

export const useAuthStore = defineStore('auth', () => {
  const api = useApi()

  const user = ref<CurrentUser | null>(null)
  const signingIn = ref(false)
  let loaded: Promise<void> | null = null

  const isAdmin = computed(() => user.value?.role === 'Admin')

  /** Loads the signed-in user once; later calls reuse the result. */
  function ensureLoaded() {
    loaded ??= api
      .get<CurrentUser>('/auth/me', { silent: true })
      .then((u) => {
        user.value = u
      })
      .catch(() => {
        user.value = null
      })
    return loaded
  }

  async function login(request: LoginRequest) {
    signingIn.value = true
    try {
      user.value = await api.post<CurrentUser>('/auth/login', { body: request, silent: true })
      loaded = Promise.resolve()
      return user.value
    } finally {
      signingIn.value = false
    }
  }

  /** Forgets the user locally (the session has ended on the server). */
  function clear() {
    user.value = null
    loaded = Promise.resolve()
  }

  async function logout() {
    await api.post('/auth/logout', { silent: true }).catch(() => undefined)
    clear()
  }

  async function changePassword(request: ChangePasswordRequest) {
    user.value = await api.post<CurrentUser>('/auth/me/password', { body: request, silent: true })
    return user.value
  }

  return { user, signingIn, isAdmin, ensureLoaded, login, clear, logout, changePassword }
})
