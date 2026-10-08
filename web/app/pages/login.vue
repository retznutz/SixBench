<script setup lang="ts">
import { ApiError } from '~/types/api-error'
import { safeRedirect } from '~/middleware/auth.global'

definePageMeta({ layout: 'blank', public: true })
useHead({ title: 'Sign in · SixBench' })

const auth = useAuthStore()
const route = useRoute()

const form = reactive({ userName: '', password: '', rememberMe: true })
const error = ref<string | null>(null)

async function submit() {
  error.value = null
  try {
    const user = await auth.login({ ...form, userName: form.userName.trim() })
    await navigateTo(user.mustChangePassword ? '/account' : safeRedirect(route.query.redirect))
  } catch (e) {
    error.value = e instanceof ApiError ? e.userMessage : 'Sign-in failed.'
    form.password = ''
  }
}
</script>

<template>
  <div class="w-full max-w-sm space-y-6">
    <div class="flex flex-col items-center gap-3 text-center">
      <BrandLogo size="lg" />
      <h1 class="text-xl font-bold">Sign in</h1>
    </div>

    <form class="space-y-4 rounded-xl border border-surface-800 p-6" @submit.prevent="submit">
      <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

      <div class="flex flex-col gap-1">
        <label for="login-user" class="text-sm">User name</label>
        <InputText
          id="login-user"
          v-model="form.userName"
          autocomplete="username"
          autocapitalize="off"
          spellcheck="false"
          required
          autofocus
        />
      </div>
      <div class="flex flex-col gap-1">
        <label for="login-password" class="text-sm">Password</label>
        <Password
          v-model="form.password"
          input-id="login-password"
          :feedback="false"
          toggle-mask
          fluid
          :input-props="{ autocomplete: 'current-password', required: true }"
        />
      </div>
      <label class="flex items-center gap-2 text-sm">
        <Checkbox v-model="form.rememberMe" input-id="login-remember" binary />
        <span>Keep me signed in</span>
      </label>
      <Button
        type="submit"
        label="Sign in"
        icon="pi pi-sign-in"
        class="w-full"
        :loading="auth.signingIn"
        :disabled="!form.userName.trim() || !form.password"
      />
    </form>
  </div>
</template>
