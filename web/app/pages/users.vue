<script setup lang="ts">
import type { User } from '~/types/user'

definePageMeta({ requiresAdmin: true })
useHead({ title: 'Users · SixBench' })

const users = useUsersStore()
const auth = useAuthStore()
const confirm = useConfirm()
const toast = useToast()

const dialogOpen = ref(false)
const dialogUser = ref<User | null>(null)
const dialogMode = ref<'edit' | 'password'>('edit')

function open(user: User | null, mode: 'edit' | 'password' = 'edit') {
  dialogUser.value = user
  dialogMode.value = mode
  dialogOpen.value = true
}

function remove(user: User) {
  confirm.require({
    header: 'Delete user?',
    message: `Delete ${user.userName}? They will be signed out within a minute.`,
    icon: 'pi pi-exclamation-triangle',
    acceptProps: { label: 'Delete', severity: 'danger' },
    rejectProps: { label: 'Cancel', severity: 'secondary', outlined: true },
    accept: async () => {
      await users.remove(user.id)
      toast.add({ severity: 'success', summary: `Deleted ${user.userName}`, life: 2500 })
    },
  })
}

function formatDate(iso: string | null) {
  return iso ? new Date(iso).toLocaleString() : 'never'
}

onMounted(() => users.fetchAll().catch(() => undefined))
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-end justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold tracking-tight">Users</h1>
        <p class="text-sm text-surface-400">
          Users can watch and control Rokus and manage devices. Admins can also manage users and HTTPS.
        </p>
      </div>
      <Button label="Add user" icon="pi pi-user-plus" @click="open(null)" />
    </div>

    <ul v-if="users.users.length" class="divide-y divide-surface-800 rounded-xl border border-surface-800">
      <li v-for="user in users.users" :key="user.id" class="flex flex-wrap items-center gap-3 p-4">
        <i class="pi pi-user text-surface-500" aria-hidden="true" />
        <div class="min-w-0 flex-1">
          <p class="truncate font-medium">
            {{ user.userName }}
            <span v-if="user.id === auth.user?.id" class="text-xs font-normal text-surface-500">(you)</span>
          </p>
          <p class="truncate text-xs text-surface-500">
            {{ user.email ?? 'no email' }} · last sign-in {{ formatDate(user.lastLoginUtc) }}
          </p>
        </div>
        <Tag v-if="user.isLockedOut" value="Locked" severity="danger" />
        <Tag v-if="user.mustChangePassword" value="Must change password" severity="warn" />
        <Tag :value="user.role" :severity="user.role === 'Admin' ? 'info' : 'secondary'" />
        <div class="flex gap-1">
          <Button
            icon="pi pi-pencil"
            text
            rounded
            severity="secondary"
            :aria-label="`Edit ${user.userName}`"
            @click="open(user)"
          />
          <Button
            icon="pi pi-key"
            text
            rounded
            severity="secondary"
            :aria-label="`Set password for ${user.userName}`"
            @click="open(user, 'password')"
          />
          <Button
            icon="pi pi-trash"
            text
            rounded
            severity="danger"
            :disabled="user.id === auth.user?.id"
            :aria-label="`Delete ${user.userName}`"
            @click="remove(user)"
          />
        </div>
      </li>
    </ul>
    <p
      v-else-if="!users.loading"
      class="rounded-xl border border-dashed border-surface-800 p-6 text-center text-sm text-surface-400"
    >
      No users.
    </p>

    <LazyUserDialog v-model:visible="dialogOpen" :user="dialogUser" :mode="dialogMode" />
  </div>
</template>
