import { defineStore } from 'pinia'
import type { CreateUserRequest, UpdateUserRequest, User } from '~/types/user'

export const useUsersStore = defineStore('users', () => {
  const api = useApi()

  const users = ref<User[]>([])
  const loading = ref(false)
  const saving = ref(false)

  async function fetchAll() {
    loading.value = true
    try {
      users.value = await api.get<User[]>('/users')
    } finally {
      loading.value = false
    }
  }

  function replace(user: User) {
    const index = users.value.findIndex((u) => u.id === user.id)
    if (index >= 0) users.value[index] = user
    else users.value = [...users.value, user].sort((a, b) => a.userName.localeCompare(b.userName))
  }

  async function withSaving<T>(action: () => Promise<T>) {
    saving.value = true
    try {
      return await action()
    } finally {
      saving.value = false
    }
  }

  /** Field errors come back as an ApiError; the dialogs show them inline, so these calls don't toast. */
  const create = (request: CreateUserRequest) =>
    withSaving(async () => {
      const user = await api.post<User>('/users', { body: request, silent: true })
      replace(user)
      return user
    })

  const update = (id: number, request: UpdateUserRequest) =>
    withSaving(async () => {
      const user = await api.put<User>(`/users/${id}`, { body: request, silent: true })
      replace(user)
      return user
    })

  const resetPassword = (id: number, newPassword: string) =>
    withSaving(async () => {
      const user = await api.post<User>(`/users/${id}/password`, { body: { newPassword }, silent: true })
      replace(user)
      return user
    })

  async function remove(id: number) {
    await api.del(`/users/${id}`)
    users.value = users.value.filter((u) => u.id !== id)
  }

  return { users, loading, saving, fetchAll, create, update, resetPassword, remove }
})
