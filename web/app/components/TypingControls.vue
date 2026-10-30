<script setup lang="ts">
/** `typing`: keys pressed on the video are being typed on the Roku right now. */
defineProps<{ typing: boolean }>()

const textEdit = useRokuTextEditStore()
</script>

<template>
  <section class="space-y-4 rounded-xl border border-surface-800 bg-surface-900/60 p-4" aria-label="Typing">
    <h2 class="text-sm font-medium text-surface-300">Typing</h2>

    <label class="flex items-center justify-between gap-3 text-sm">
      <span class="flex items-center gap-2">
        <i class="pi pi-pencil text-surface-400" aria-hidden="true" />
        Live typing
      </span>
      <ToggleSwitch v-model="textEdit.liveTyping" input-id="live-typing" aria-describedby="live-typing-note" />
    </label>
    <p id="live-typing-note" class="-mt-2 text-xs text-surface-500">
      Each key is sent as you press it. When a text field is selected on the Roku, type on the video: Space and letters
      are typed instead of acting as shortcuts, the arrows, Enter and Backspace still work, and Esc leaves the field.
    </p>

    <p v-if="typing" class="flex items-center gap-2 text-xs text-primary-400" role="status">
      <i class="pi pi-circle-fill text-[0.5rem]" aria-hidden="true" />
      Text field selected — typing on the Roku
    </p>
  </section>
</template>
