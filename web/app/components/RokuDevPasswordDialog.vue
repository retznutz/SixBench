<script setup lang="ts">
import { ApiError } from '~/types/api-error'
import type { RokuDevice } from '~/types/roku-device'

/** Saves (after the Roku accepts it) or clears a Roku's developer-mode password. Admin only. */
const visible = defineModel<boolean>('visible', { required: true })
const props = defineProps<{ roku: RokuDevice | null }>()
const emit = defineEmits<{ saved: [] }>()

const rokus = useRokuDevicesStore()
const toast = useToast()

const password = ref('')
const error = ref<string | null>(null)
const saving = ref(false)
const clearing = ref(false)

watch(visible, (open) => {
  if (!open) return
  password.value = ''
  error.value = null
})

async function save() {
  if (!props.roku || !password.value) return
  saving.value = true
  error.value = null
  try {
    await rokus.setDevPassword(props.roku.id, password.value)
    toast.add({ severity: 'success', summary: 'Developer password saved', life: 3000 })
    visible.value = false
    emit('saved')
  } catch (e) {
    error.value = e instanceof ApiError ? e.userMessage : 'Could not save the password.'
  } finally {
    saving.value = false
  }
}

async function clear() {
  if (!props.roku) return
  clearing.value = true
  try {
    await rokus.clearDevPassword(props.roku.id)
    toast.add({ severity: 'info', summary: 'Developer password removed', life: 3000 })
    visible.value = false
    emit('saved')
  } catch {
    // Toast already shown.
  } finally {
    clearing.value = false
  }
}
</script>

<template>
  <Dialog
    v-model:visible="visible"
    modal
    :header="`Developer password · ${roku?.friendlyName ?? 'Roku'}`"
    class="w-[min(30rem,calc(100vw-2rem))]"
  >
    <form class="flex flex-col gap-4" @submit.prevent="save">
      <p class="text-sm text-surface-400">
        The password you chose when you enabled developer mode on this Roku (the web page's user is
        <span class="font-mono">rokudev</span>). SixBench checks it with the Roku, then stores it encrypted so it can
        sideload, package and screenshot for you.
      </p>

      <div class="flex flex-col gap-1">
        <label for="roku-dev-password" class="text-sm">Password</label>
        <Password
          v-model="password"
          input-id="roku-dev-password"
          :feedback="false"
          toggle-mask
          fluid
          :invalid="!!error"
          :input-props="{ autocomplete: 'off', required: true }"
        />
        <small v-if="error" class="text-red-400">{{ error }}</small>
      </div>

      <p class="text-xs text-surface-500">
        Forgot it? Enter the developer mode sequence on the remote again (Home ×3, Up ×2, Right, Left, Right, Left,
        Right) to set a new one.
      </p>

      <div class="flex flex-wrap items-center justify-end gap-2">
        <Button
          v-if="roku?.hasDevPassword"
          label="Remove saved password"
          severity="danger"
          text
          class="mr-auto"
          :loading="clearing"
          @click="clear"
        />
        <Button label="Cancel" severity="secondary" text @click="visible = false" />
        <Button type="submit" label="Check and save" icon="pi pi-check" :loading="saving" :disabled="!password" />
      </div>
    </form>
  </Dialog>
</template>
