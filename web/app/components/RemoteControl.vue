<script setup lang="ts">
import type { RokuKey } from '~/types/remote'

const props = defineProps<{ rokuId: number | null; rokuName?: string | null }>()
const emit = defineEmits<{ openText: [] }>()

const remote = useRemoteStore()
const disabled = computed(() => props.rokuId == null)

interface RemoteButton {
  key: RokuKey
  icon: string
  label: string
}

const topRow: RemoteButton[] = [
  { key: 'Power', icon: 'pi pi-power-off', label: 'Power' },
  { key: 'Back', icon: 'pi pi-arrow-left', label: 'Back' },
  { key: 'Home', icon: 'pi pi-home', label: 'Home' },
]

const rows: RemoteButton[][] = [
  [
    { key: 'InstantReplay', icon: 'pi pi-replay', label: 'Instant replay' },
    { key: 'Info', icon: 'pi pi-asterisk', label: 'Options (*)' },
    { key: 'Search', icon: 'pi pi-search', label: 'Search' },
  ],
  [
    { key: 'Rev', icon: 'pi pi-backward', label: 'Rewind' },
    { key: 'Play', icon: 'pi pi-play', label: 'Play / pause' },
    { key: 'Fwd', icon: 'pi pi-forward', label: 'Fast forward' },
  ],
  [
    { key: 'VolumeDown', icon: 'pi pi-volume-down', label: 'Volume down' },
    { key: 'VolumeMute', icon: 'pi pi-volume-off', label: 'Mute' },
    { key: 'VolumeUp', icon: 'pi pi-volume-up', label: 'Volume up' },
  ],
]

function press(key: RokuKey) {
  if (props.rokuId == null) return
  // Errors are already surfaced as toasts by the API layer.
  remote.sendKey(props.rokuId, key).catch(() => undefined)
}
</script>

<template>
  <section class="rounded-xl border border-surface-800 bg-surface-900/60 p-4" aria-label="Roku remote">
    <header class="mb-4 flex min-w-0 items-baseline justify-between gap-2">
      <h2 class="text-sm font-medium text-surface-300">Remote</h2>
      <span v-if="rokuName" class="min-w-0 truncate text-xs text-surface-500" :title="rokuName">{{ rokuName }}</span>
    </header>

    <Message v-if="disabled" severity="warn" size="small" class="mb-4">
      No Roku is linked to this encoder.
      <NuxtLink to="/settings" class="underline">Link one in Settings</NuxtLink>.
    </Message>

    <fieldset
      :disabled="disabled"
      class="mx-auto flex w-44 flex-col items-stretch gap-4"
      :class="{ 'opacity-50': disabled }"
    >
      <div class="grid grid-cols-3 justify-items-center">
        <Button
          v-for="b in topRow"
          :key="b.key"
          v-tooltip.top="b.label"
          :icon="b.icon"
          :severity="b.key === 'Power' ? 'danger' : 'secondary'"
          :text="b.key === 'Power'"
          rounded
          :aria-label="b.label"
          @click="press(b.key)"
        />
      </div>

      <div
        class="grid aspect-square w-full grid-cols-3 grid-rows-3 gap-1 rounded-full bg-surface-800/60 p-2"
        role="group"
        aria-label="Direction pad"
      >
        <span />
        <Button icon="pi pi-chevron-up" text rounded aria-label="Up" class="!size-full" @click="press('Up')" />
        <span />
        <Button icon="pi pi-chevron-left" text rounded aria-label="Left" class="!size-full" @click="press('Left')" />
        <Button label="OK" rounded aria-label="OK" class="!size-full !font-semibold" @click="press('Select')" />
        <Button icon="pi pi-chevron-right" text rounded aria-label="Right" class="!size-full" @click="press('Right')" />
        <span />
        <Button icon="pi pi-chevron-down" text rounded aria-label="Down" class="!size-full" @click="press('Down')" />
        <span />
      </div>

      <div class="grid grid-cols-3 justify-items-center gap-y-3">
        <template v-for="row in rows" :key="row[0]!.key">
          <Button
            v-for="b in row"
            :key="b.key"
            v-tooltip.top="b.label"
            :icon="b.icon"
            severity="secondary"
            text
            rounded
            class="!bg-surface-800/60 hover:!bg-surface-700/80"
            :aria-label="b.label"
            @click="press(b.key)"
          />
        </template>
      </div>
    </fieldset>

    <Button
      label="Type text"
      icon="pi pi-pencil"
      severity="secondary"
      size="small"
      outlined
      class="mt-4 w-full"
      :disabled="disabled"
      @click="emit('openText')"
    />
  </section>
</template>
