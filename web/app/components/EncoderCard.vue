<script setup lang="ts">
import type { CaptureDevice } from '~/types/capture-device'

const props = defineProps<{ device: CaptureDevice }>()

const devices = useCaptureDevicesStore()
const name = computed(() => devices.displayName(props.device))
const roku = computed(() => props.device.link?.rokuDevice ?? null)
</script>

<template>
  <article class="flex flex-col overflow-hidden rounded-xl border border-zinc-800 bg-zinc-900/60">
    <NuxtLink
      :to="device.isConnected ? `/watch/${device.id}` : undefined"
      class="group relative grid aspect-video place-items-center bg-zinc-950"
      :class="device.isConnected ? 'cursor-pointer' : 'cursor-not-allowed opacity-60'"
      :aria-label="device.isConnected ? `Watch ${name}` : `${name} is unplugged`"
    >
      <i class="pi pi-video text-4xl text-zinc-700 transition-colors group-hover:text-primary" aria-hidden="true" />
      <span
        v-if="device.isConnected"
        class="absolute inset-0 grid place-items-center bg-zinc-950/60 opacity-0 transition-opacity group-hover:opacity-100"
      >
        <span
          class="flex items-center gap-2 rounded-full bg-primary px-4 py-2 text-sm font-medium text-primary-contrast"
        >
          <i class="pi pi-play" aria-hidden="true" /> Watch
        </span>
      </span>
    </NuxtLink>

    <div class="flex flex-1 flex-col gap-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h2 class="min-w-0 truncate font-medium text-zinc-100" :title="name">{{ name }}</h2>
        <Tag
          :value="device.isConnected ? 'Connected' : 'Unplugged'"
          :severity="device.isConnected ? 'success' : 'secondary'"
          class="shrink-0"
        />
      </div>

      <p v-if="device.link && device.link.displayName !== device.name" class="-mt-2 truncate text-xs text-zinc-500">
        {{ device.name }}
      </p>

      <ul class="space-y-1 text-sm text-zinc-400">
        <li class="flex items-center gap-2">
          <i class="pi pi-link text-xs" aria-hidden="true" />
          <span v-if="roku" class="truncate">{{ roku.friendlyName }} · {{ roku.ipAddress }}</span>
          <span v-else class="text-amber-400/90">No Roku linked</span>
        </li>
        <li class="flex items-center gap-2">
          <i class="pi pi-volume-up text-xs" aria-hidden="true" />
          <span v-if="device.link?.allowDeviceAudio && device.audioInput" class="truncate">{{
            device.audioInput
          }}</span>
          <span v-else>Audio off</span>
        </li>
      </ul>

      <div class="mt-auto flex gap-2 pt-1">
        <NuxtLink :to="device.isConnected ? `/watch/${device.id}` : undefined" class="flex-1">
          <Button label="Watch" icon="pi pi-play" size="small" class="w-full" :disabled="!device.isConnected" />
        </NuxtLink>
        <NuxtLink :to="{ path: '/settings', query: { device: device.id } }">
          <Button
            v-tooltip.top="'Configure'"
            icon="pi pi-sliders-h"
            size="small"
            severity="secondary"
            outlined
            aria-label="Configure encoder"
          />
        </NuxtLink>
      </div>
    </div>
  </article>
</template>
