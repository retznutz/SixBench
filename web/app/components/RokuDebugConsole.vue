<script setup lang="ts">
import { downloadBlob, snapshotFileName } from '~/lib/stream/snapshot'

/** The BrightScript debug console (port 8085), shared with everyone else watching it. Admin only. */
const props = defineProps<{ rokuId: number }>()

const debugConsole = useRokuConsoleStore()

const output = ref<HTMLElement | null>(null)
const line = ref('')
const follow = ref(true)

/** Debugger commands worth a button; the console accepts any. */
const quickCommands = [
  { command: 'bt', label: 'bt', title: 'Backtrace' },
  { command: 'var', label: 'var', title: 'Local variables' },
  { command: 'step', label: 'step', title: 'Step one statement' },
  { command: 'over', label: 'over', title: 'Step over' },
  { command: 'cont', label: 'cont', title: 'Continue running' },
]

const stateTag = computed(() => {
  switch (debugConsole.status?.state) {
    case 'Connected':
      return { value: 'Connected', severity: 'success' }
    case 'Connecting':
      return { value: 'Connecting', severity: 'warn' }
    case 'Disconnected':
      return { value: 'Disconnected', severity: 'danger' }
    default:
      return { value: debugConsole.connected ? 'Idle' : 'Offline', severity: 'secondary' }
  }
})

watch(
  () => props.rokuId,
  (id) => debugConsole.watch(id),
  { immediate: true },
)
onBeforeUnmount(() => debugConsole.unwatch())

// Keep the newest output in view unless the user scrolled up to read.
watch(
  () => debugConsole.text,
  async () => {
    if (!follow.value) return
    await nextTick()
    if (output.value) output.value.scrollTop = output.value.scrollHeight
  },
)

function onScroll() {
  const el = output.value
  if (el) follow.value = el.scrollHeight - el.scrollTop - el.clientHeight < 24
}

async function send(text = line.value) {
  if (await debugConsole.send(text)) {
    if (text === line.value) line.value = ''
    follow.value = true
  }
}

function save() {
  downloadBlob(
    new Blob([debugConsole.text], { type: 'text/plain' }),
    snapshotFileName('roku-console', new Date(), 'log'),
  )
}
</script>

<template>
  <div class="space-y-2">
    <div class="flex flex-wrap items-center gap-2 text-xs text-surface-400">
      <Tag :value="stateTag.value" :severity="stateTag.severity" />
      <span class="min-w-0 flex-1 truncate" :title="debugConsole.status?.message ?? undefined">
        {{ debugConsole.status?.message ?? 'BrightScript output, crash logs and the debugger prompt (port 8085).' }}
      </span>
      <Button
        v-tooltip.bottom="'Save the output as a .log file'"
        icon="pi pi-download"
        text
        rounded
        size="small"
        severity="secondary"
        aria-label="Save console output"
        :disabled="!debugConsole.text"
        @click="save"
      />
      <Button
        v-tooltip.bottom="'Clear (this browser only)'"
        icon="pi pi-eraser"
        text
        rounded
        size="small"
        severity="secondary"
        aria-label="Clear console output"
        @click="debugConsole.clear()"
      />
    </div>

    <Message v-if="debugConsole.error" severity="warn" size="small">{{ debugConsole.error }}</Message>

    <pre
      ref="output"
      class="h-80 resize-y overflow-auto whitespace-pre-wrap break-all rounded-lg border border-surface-800 bg-black/60 p-3 font-mono text-[11px] leading-relaxed text-surface-200"
      aria-live="off"
      @scroll="onScroll"
      >{{ debugConsole.text || 'Waiting for output…' }}</pre>

    <form class="flex flex-wrap items-center gap-2" @submit.prevent="send()">
      <InputText
        v-model="line"
        size="small"
        class="min-w-40 flex-1 font-mono"
        placeholder="Debugger command, e.g. bt"
        aria-label="Console input"
        autocomplete="off"
        spellcheck="false"
        :disabled="debugConsole.status?.state !== 'Connected'"
      />
      <Button
        type="submit"
        label="Send"
        icon="pi pi-send"
        size="small"
        :disabled="debugConsole.status?.state !== 'Connected' || !line"
      />
      <span class="flex gap-1">
        <Button
          v-for="q in quickCommands"
          :key="q.command"
          v-tooltip.bottom="q.title"
          :label="q.label"
          size="small"
          severity="secondary"
          text
          class="font-mono"
          :disabled="debugConsole.status?.state !== 'Connected'"
          @click="send(q.command)"
        />
      </span>
    </form>
    <p class="text-xs text-surface-500">
      The Roku allows one connection to this debugConsole. SixBench shares it with everyone watching, but a VS Code
      debugger or another telnet session will keep it from connecting.
    </p>
  </div>
</template>
