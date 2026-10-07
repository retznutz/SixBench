<script setup lang="ts">
import { ApiError } from '~/types/api-error'

useHead({ title: 'Account · SixBench' })

const auth = useAuthStore()
const toast = useToast()

const form = reactive({ currentPassword: '', newPassword: '', confirm: '' })
const errors = ref<Record<string, string[]>>({})
const saving = ref(false)

const mismatch = computed(() => !!form.confirm && form.confirm !== form.newPassword)

function fieldError(name: string): string | undefined {
  const key = Object.keys(errors.value).find((k) => k.toLowerCase() === name.toLowerCase())
  return key ? errors.value[key]?.join(' ') : undefined
}

async function submit() {
  if (mismatch.value) return
  errors.value = {}
  saving.value = true
  const wasForced = auth.user?.mustChangePassword
  try {
    await auth.changePassword({ currentPassword: form.currentPassword, newPassword: form.newPassword })
    form.currentPassword = form.newPassword = form.confirm = ''
    toast.add({ severity: 'success', summary: 'Password changed', life: 3000 })
    if (wasForced) await navigateTo('/')
  } catch (e) {
    if (e instanceof ApiError && e.errors) errors.value = e.errors
    else
      toast.add({ severity: 'error', summary: 'Could not change password', detail: (e as Error).message, life: 6000 })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="mx-auto max-w-md space-y-6">
    <div>
      <h1 class="text-2xl font-semibold tracking-tight">Account</h1>
      <p class="text-sm text-zinc-400">
        Signed in as <span class="font-medium text-zinc-200">{{ auth.user?.userName }}</span> ·
        {{ auth.user?.role === 'Admin' ? 'Administrator' : 'User' }}
      </p>
    </div>

    <Message v-if="auth.user?.mustChangePassword" severity="warn" :closable="false">
      Choose a new password before continuing.
    </Message>

    <form class="space-y-4 rounded-xl border border-zinc-800 p-6" @submit.prevent="submit">
      <h2 class="text-lg font-medium">Change password</h2>
      <div class="flex flex-col gap-1">
        <label for="pw-current" class="text-sm">Current password</label>
        <Password
          v-model="form.currentPassword"
          input-id="pw-current"
          :feedback="false"
          toggle-mask
          fluid
          :invalid="!!fieldError('currentPassword')"
          :input-props="{ autocomplete: 'current-password', required: true }"
        />
        <small v-if="fieldError('currentPassword')" class="text-red-400">{{ fieldError('currentPassword') }}</small>
      </div>
      <div class="flex flex-col gap-1">
        <label for="pw-new" class="text-sm">New password</label>
        <Password
          v-model="form.newPassword"
          input-id="pw-new"
          toggle-mask
          fluid
          :invalid="!!fieldError('newPassword')"
          :input-props="{ autocomplete: 'new-password', required: true }"
        />
        <small v-if="fieldError('newPassword')" class="text-red-400">{{ fieldError('newPassword') }}</small>
        <small v-else class="text-zinc-500">
          At least 8 characters, with upper- and lower-case letters, a digit and a symbol.
        </small>
      </div>
      <div class="flex flex-col gap-1">
        <label for="pw-confirm" class="text-sm">Confirm new password</label>
        <Password
          v-model="form.confirm"
          input-id="pw-confirm"
          :feedback="false"
          toggle-mask
          fluid
          :invalid="mismatch"
          :input-props="{ autocomplete: 'new-password', required: true }"
        />
        <small v-if="mismatch" class="text-red-400">The passwords don't match.</small>
      </div>
      <Button
        type="submit"
        label="Change password"
        icon="pi pi-key"
        :loading="saving"
        :disabled="!form.currentPassword || !form.newPassword || !form.confirm || mismatch"
      />
    </form>
  </div>
</template>
