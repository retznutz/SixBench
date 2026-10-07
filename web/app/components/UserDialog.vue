<script setup lang="ts">
import { ApiError } from '~/types/api-error'
import type { Role } from '~/types/auth'
import type { User } from '~/types/user'

/** `user` null = create; otherwise edit. `mode: 'password'` sets a new password for `user`. */
const visible = defineModel<boolean>('visible', { required: true })
const props = defineProps<{ user: User | null; mode: 'edit' | 'password' }>()

const users = useUsersStore()
const toast = useToast()

const form = reactive({ userName: '', email: '' as string | null, password: '', role: 'User' as Role })
const errors = ref<Record<string, string[]>>({})

const roleOptions = [
  { label: 'User: watch, control and manage devices', value: 'User' },
  { label: 'Admin: everything, including users and HTTPS', value: 'Admin' },
]

const creating = computed(() => props.mode === 'edit' && !props.user)
const header = computed(() =>
  props.mode === 'password'
    ? `Set password for ${props.user?.userName}`
    : creating.value
      ? 'Add user'
      : `Edit ${props.user?.userName}`,
)

watch(
  () => [visible.value, props.user, props.mode] as const,
  ([open, user]) => {
    if (!open) return
    form.userName = user?.userName ?? ''
    form.email = user?.email ?? null
    form.password = ''
    form.role = user?.role ?? 'User'
    errors.value = {}
  },
  { immediate: true },
)

function fieldError(name: string): string | undefined {
  const key = Object.keys(errors.value).find((k) => k.toLowerCase() === name.toLowerCase())
  return key ? errors.value[key]?.join(' ') : undefined
}

async function save() {
  errors.value = {}
  const email = form.email?.trim() || null
  try {
    if (props.mode === 'password' && props.user) {
      await users.resetPassword(props.user.id, form.password)
      toast.add({
        severity: 'success',
        summary: 'Password set',
        detail: 'They must change it at next sign-in.',
        life: 4000,
      })
    } else if (props.user) {
      await users.update(props.user.id, { email, role: form.role })
      toast.add({ severity: 'success', summary: 'User saved', life: 2500 })
    } else {
      const user = await users.create({
        userName: form.userName.trim(),
        email,
        password: form.password,
        role: form.role,
      })
      toast.add({ severity: 'success', summary: `Added ${user.userName}`, life: 3000 })
    }
    visible.value = false
  } catch (e) {
    if (e instanceof ApiError && e.errors) {
      errors.value = e.errors
    } else {
      const error = e instanceof ApiError ? e : null
      toast.add({ severity: 'error', summary: error?.title ?? 'Save failed', detail: error?.userMessage, life: 6000 })
    }
  }
}
</script>

<template>
  <Dialog v-model:visible="visible" modal :header="header" class="w-[min(28rem,calc(100vw-2rem))]">
    <form class="flex flex-col gap-4" @submit.prevent="save">
      <small v-if="fieldError('')" class="text-red-400">{{ fieldError('') }}</small>

      <div v-if="creating" class="flex flex-col gap-1">
        <label for="user-name" class="text-sm">User name</label>
        <InputText
          id="user-name"
          v-model="form.userName"
          maxlength="256"
          autocomplete="off"
          autocapitalize="off"
          spellcheck="false"
          required
          :invalid="!!fieldError('userName')"
        />
        <small v-if="fieldError('userName')" class="text-red-400">{{ fieldError('userName') }}</small>
      </div>

      <template v-if="mode === 'edit'">
        <div class="flex flex-col gap-1">
          <label for="user-email" class="text-sm">Email (optional)</label>
          <InputText
            id="user-email"
            v-model="form.email"
            type="email"
            maxlength="256"
            :invalid="!!fieldError('email')"
          />
          <small v-if="fieldError('email')" class="text-red-400">{{ fieldError('email') }}</small>
        </div>
        <div class="flex flex-col gap-1">
          <label for="user-role" class="text-sm">Role</label>
          <Select
            v-model="form.role"
            input-id="user-role"
            :options="roleOptions"
            option-label="label"
            option-value="value"
            :invalid="!!fieldError('role')"
          />
          <small v-if="fieldError('role')" class="text-red-400">{{ fieldError('role') }}</small>
        </div>
      </template>

      <div v-if="creating || mode === 'password'" class="flex flex-col gap-1">
        <label for="user-password" class="text-sm">{{ creating ? 'Initial password' : 'New password' }}</label>
        <Password
          v-model="form.password"
          input-id="user-password"
          toggle-mask
          fluid
          :invalid="!!fieldError('password') || !!fieldError('newPassword')"
          :input-props="{ autocomplete: 'new-password', required: true }"
        />
        <small v-if="fieldError('password') || fieldError('newPassword')" class="text-red-400">
          {{ fieldError('password') ?? fieldError('newPassword') }}
        </small>
        <small v-else class="text-zinc-500">They'll be asked to change it when they sign in.</small>
      </div>

      <div class="flex justify-end gap-2">
        <Button label="Cancel" severity="secondary" text @click="visible = false" />
        <Button type="submit" :label="creating ? 'Add user' : 'Save'" icon="pi pi-check" :loading="users.saving" />
      </div>
    </form>
  </Dialog>
</template>
