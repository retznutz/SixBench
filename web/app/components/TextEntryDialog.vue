<script setup lang="ts">
const visible = defineModel<boolean>('visible', { required: true })
const props = defineProps<{ rokuId: number | null }>()

const remote = useRemoteStore()
const text = ref('')
const live = ref(false)
const sending = ref(false)

async function sendAll() {
  if (props.rokuId == null || !text.value) return
  sending.value = true
  try {
    await remote.sendText(props.rokuId, text.value)
    text.value = ''
  } catch {
    // Toast already shown.
  } finally {
    sending.value = false
  }
}

function sendKey(key: 'Backspace' | 'Enter' | 'Search') {
  if (props.rokuId != null) remote.sendKey(props.rokuId, key).catch(() => undefined)
}

/** Live mode: every keystroke goes straight to the Roku's on-screen keyboard. */
function onLiveKeydown(event: KeyboardEvent) {
  if (!live.value || props.rokuId == null || event.ctrlKey || event.metaKey || event.altKey) return
  if (event.key === 'Backspace') {
    event.preventDefault()
    sendKey('Backspace')
  } else if (event.key === 'Enter') {
    event.preventDefault()
    sendKey('Enter')
  } else if (event.key.length === 1) {
    event.preventDefault()
    remote.sendText(props.rokuId, event.key).catch(() => undefined)
  }
}
</script>

<template>
  <Dialog v-model:visible="visible" modal header="Type on the Roku" class="w-[min(28rem,calc(100vw-2rem))]">
    <div class="flex flex-col gap-4">
      <label class="flex items-center gap-3 text-sm text-surface-300">
        <ToggleSwitch v-model="live" input-id="live-typing" />
        <span>Live typing <span class="text-surface-500">— each key is sent as you type</span></span>
      </label>

      <InputText
        v-if="live"
        autofocus
        placeholder="Start typing…"
        class="w-full"
        aria-label="Live typing"
        :model-value="''"
        @keydown="onLiveKeydown"
      />
      <form v-else class="flex gap-2" @submit.prevent="sendAll">
        <InputText
          v-model="text"
          autofocus
          placeholder="Text to send"
          class="flex-1"
          maxlength="256"
          aria-label="Text to send"
        />
        <Button type="submit" label="Send" icon="pi pi-send" :loading="sending" :disabled="!text" />
      </form>

      <div class="grid grid-cols-3 gap-2">
        <Button
          label="Backspace"
          icon="pi pi-delete-left"
          severity="secondary"
          outlined
          size="small"
          @click="sendKey('Backspace')"
        />
        <Button
          label="Search"
          icon="pi pi-search"
          severity="secondary"
          outlined
          size="small"
          @click="sendKey('Search')"
        />
        <Button label="Enter" icon="pi pi-check" severity="secondary" outlined size="small" @click="sendKey('Enter')" />
      </div>
      <p class="text-xs text-surface-500">Open a search box on the Roku first. Text is sent one character at a time.</p>
    </div>
  </Dialog>
</template>
