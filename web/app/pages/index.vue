<script setup lang="ts">
useHead({ title: 'Encoders · SixBench' })

const devices = useCaptureDevicesStore()

onMounted(() => {
  devices.fetchAll().catch(() => undefined)
})
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold tracking-tight">Encoders</h1>
        <p class="text-sm text-surface-400">
          HDMI capture devices attached to this server. Pick one to watch and control its Roku.
        </p>
      </div>
      <Button
        label="Rescan"
        icon="pi pi-refresh"
        severity="secondary"
        outlined
        size="small"
        :loading="devices.loading"
        @click="devices.fetchAll(true).catch(() => undefined)"
      />
    </div>

    <div v-if="!devices.loaded && devices.loading" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <Skeleton v-for="n in 3" :key="n" height="18rem" border-radius="0.75rem" />
    </div>

    <div
      v-else-if="devices.devices.length === 0"
      class="rounded-xl border border-dashed border-surface-800 p-10 text-center"
    >
      <i class="pi pi-video mb-3 text-3xl text-surface-600" aria-hidden="true" />
      <h2 class="font-medium">No capture devices found</h2>
      <ul class="mx-auto mt-3 max-w-md space-y-1 text-left text-sm text-surface-400">
        <li>• Plug in the HDMI-to-USB encoder and click Rescan.</li>
        <li>• Make sure ffmpeg is installed (or set <code>Ffmpeg:Path</code> in appsettings.json).</li>
        <li>• On macOS, allow camera access for the app running the server.</li>
      </ul>
    </div>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <EncoderCard v-for="device in devices.devices" :key="device.id" :device="device" />
    </div>
  </div>
</template>
