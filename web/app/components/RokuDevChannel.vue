<script setup lang="ts">
/** Sideloaded channel: status, install from a zip, launch, screenshot, delete and the Roku utilities. */
const props = defineProps<{ rokuId: number }>()

const devChannel = useRokuDevChannelStore()
const rokus = useRokuDevicesStore()
const auth = useAuthStore()
const confirm = useConfirm()

const fileInput = ref<HTMLInputElement | null>(null)
const archive = ref<File | null>(null)
const dragging = ref(false)
const passwordOpen = ref(false)

const roku = computed(() => rokus.byId(props.rokuId) ?? null)
const status = computed(() => devChannel.status)
const hasPassword = computed(() => status.value?.hasDevPassword ?? roku.value?.hasDevPassword ?? false)
const busy = computed(() => devChannel.busy != null)

function refresh() {
  devChannel.fetchStatus(props.rokuId)
}

watch(() => props.rokuId, refresh)
onMounted(async () => {
  refresh()
  if (!roku.value) await rokus.fetchAll().catch(() => undefined)
})

function pick(files: FileList | null | undefined) {
  const file = files?.[0]
  if (file) archive.value = file
}

function onDrop(event: DragEvent) {
  dragging.value = false
  pick(event.dataTransfer?.files)
}

async function install() {
  if (!archive.value) return
  const result = await devChannel.install(props.rokuId, archive.value)
  if (result && fileInput.value) fileInput.value.value = ''
}

function confirmDelete() {
  confirm.require({
    header: 'Delete the sideloaded channel?',
    message: 'This removes the dev channel from the Roku. Its registry data is deleted too.',
    icon: 'pi pi-exclamation-triangle',
    acceptProps: { label: 'Delete', severity: 'danger' },
    rejectProps: { label: 'Cancel', severity: 'secondary', outlined: true },
    accept: () => devChannel.remove(props.rokuId),
  })
}

function confirmReboot() {
  confirm.require({
    header: 'Reboot the Roku?',
    message: 'The video goes dark for about a minute while it restarts.',
    icon: 'pi pi-power-off',
    acceptProps: { label: 'Reboot', severity: 'danger' },
    rejectProps: { label: 'Cancel', severity: 'secondary', outlined: true },
    accept: () => devChannel.reboot(props.rokuId),
  })
}

function formatSize(bytes: number) {
  return bytes >= 1024 * 1024 ? `${(bytes / (1024 * 1024)).toFixed(1)} MB` : `${Math.ceil(bytes / 1024)} KB`
}
</script>

<template>
  <div class="space-y-4">
    <!-- Status -->
    <div class="flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-surface-400">
      <span class="flex items-center gap-1.5">
        Developer mode
        <Tag
          :value="status?.developerModeEnabled == null ? 'Unknown' : status.developerModeEnabled ? 'On' : 'Off'"
          :severity="
            status?.developerModeEnabled ? 'success' : status?.developerModeEnabled === false ? 'danger' : 'secondary'
          "
        />
      </span>
      <span class="flex min-w-0 items-center gap-1.5">
        Sideloaded
        <span v-if="status?.devChannel" class="truncate text-surface-200">
          {{ status.devChannel.name
          }}<template v-if="status.devChannel.version"> v{{ status.devChannel.version }}</template>
        </span>
        <span v-else class="text-surface-500">none</span>
      </span>
      <span class="flex min-w-0 items-center gap-1.5">
        Signing key
        <span v-if="status?.keyedDeveloperId" class="truncate font-mono text-surface-200">
          {{ status.keyedDeveloperId }}
        </span>
        <span v-else class="text-surface-500">none</span>
      </span>
      <span class="flex-1" />
      <Button
        v-tooltip.bottom="'Refresh'"
        icon="pi pi-refresh"
        text
        rounded
        size="small"
        severity="secondary"
        aria-label="Refresh developer status"
        :loading="devChannel.statusLoading"
        @click="refresh"
      />
    </div>

    <Message v-if="devChannel.statusError" severity="warn" size="small">
      <strong>{{ devChannel.statusError.title }}.</strong> {{ devChannel.statusError.userMessage }}
    </Message>

    <Message v-if="status?.developerModeEnabled === false" severity="secondary" size="small">
      Developer mode is off. On the Roku remote press Home ×3, Up ×2, Right, Left, Right, Left, Right, then follow the
      steps and choose a password.
    </Message>

    <Message v-else-if="!hasPassword" severity="secondary" size="small">
      <div class="flex flex-wrap items-center gap-2">
        <span class="flex-1">
          Save this Roku's developer password to sideload, screenshot and package from here.
          <template v-if="!auth.isAdmin">Ask an administrator to set it.</template>
        </span>
        <Button
          v-if="auth.isAdmin && roku"
          label="Set password"
          icon="pi pi-key"
          size="small"
          @click="passwordOpen = true"
        />
      </div>
    </Message>

    <!-- Install -->
    <section v-if="auth.isAdmin" class="space-y-2" aria-labelledby="dev-install-heading">
      <h3 id="dev-install-heading" class="text-xs font-medium uppercase tracking-wide text-surface-500">
        Install a build
      </h3>
      <div
        class="flex flex-wrap items-center gap-3 rounded-lg border border-dashed p-3 transition-colors"
        :class="dragging ? 'border-primary bg-primary/5' : 'border-surface-700'"
        @dragover.prevent="dragging = true"
        @dragleave="dragging = false"
        @drop.prevent="onDrop"
      >
        <input
          ref="fileInput"
          type="file"
          accept=".zip,application/zip"
          class="hidden"
          @change="pick(($event.target as HTMLInputElement).files)"
        />
        <Button
          label="Choose zip"
          icon="pi pi-folder-open"
          size="small"
          severity="secondary"
          outlined
          :disabled="busy"
          @click="fileInput?.click()"
        />
        <span v-if="archive" class="min-w-0 truncate text-sm">
          {{ archive.name }} <span class="text-surface-500">({{ formatSize(archive.size) }})</span>
        </span>
        <span v-else class="text-sm text-surface-500">or drop a channel .zip here</span>
        <span class="flex-1" />
        <Button
          label="Install"
          icon="pi pi-upload"
          size="small"
          :loading="devChannel.busy === 'install'"
          :disabled="!archive || !hasPassword || busy"
          @click="install"
        />
      </div>
      <p class="text-xs text-surface-500">
        Replaces the current dev channel and launches it. The manifest must be at the root of the zip.
      </p>
    </section>

    <!-- Actions -->
    <section class="space-y-2" aria-labelledby="dev-actions-heading">
      <h3 id="dev-actions-heading" class="text-xs font-medium uppercase tracking-wide text-surface-500">
        Sideloaded channel
      </h3>
      <div class="flex flex-wrap gap-2">
        <Button
          label="Launch"
          icon="pi pi-play"
          size="small"
          severity="secondary"
          :loading="devChannel.busy === 'launch'"
          :disabled="busy || !status?.devChannel"
          @click="devChannel.launch(rokuId)"
        />
        <Button
          v-tooltip.bottom="'Rendered by the Roku itself; only works while the dev channel is running'"
          label="Screenshot"
          icon="pi pi-camera"
          size="small"
          severity="secondary"
          :loading="devChannel.busy === 'screenshot'"
          :disabled="busy || !hasPassword"
          @click="devChannel.screenshot(rokuId)"
        />
        <template v-if="auth.isAdmin">
          <Button
            v-tooltip.bottom="'Faster start-up; needed for large channels'"
            label="Convert to squashfs"
            icon="pi pi-box"
            size="small"
            severity="secondary"
            :loading="devChannel.busy === 'squashfs'"
            :disabled="busy || !hasPassword || !status?.devChannel"
            @click="devChannel.convertToSquashfs(rokuId)"
          />
          <Button
            label="Delete"
            icon="pi pi-trash"
            size="small"
            severity="danger"
            outlined
            :loading="devChannel.busy === 'delete'"
            :disabled="busy || !hasPassword || !status?.devChannel"
            @click="confirmDelete"
          />
        </template>
      </div>
    </section>

    <!-- Utilities -->
    <section v-if="auth.isAdmin" class="space-y-2" aria-labelledby="dev-utilities-heading">
      <h3 id="dev-utilities-heading" class="text-xs font-medium uppercase tracking-wide text-surface-500">Roku</h3>
      <div class="flex flex-wrap gap-2">
        <Button
          v-tooltip.bottom="'Some Roku OS versions refuse installs until they have checked for an update'"
          label="Check for update"
          icon="pi pi-sync"
          size="small"
          severity="secondary"
          :loading="devChannel.busy === 'check-update'"
          :disabled="busy || !hasPassword"
          @click="devChannel.checkForUpdate(rokuId)"
        />
        <Button
          label="Reboot"
          icon="pi pi-power-off"
          size="small"
          severity="secondary"
          :loading="devChannel.busy === 'reboot'"
          :disabled="busy || !hasPassword"
          @click="confirmReboot"
        />
      </div>
    </section>

    <RokuDevResult :actions="['install', 'delete', 'launch', 'squashfs', 'screenshot', 'reboot', 'check-update']" />

    <figure v-if="devChannel.screenshotUrl" class="space-y-2">
      <img
        :src="devChannel.screenshotUrl"
        alt="Screenshot of the sideloaded channel, rendered by the Roku"
        class="w-full rounded-lg border border-surface-800"
      />
      <figcaption class="flex items-center gap-2 text-xs text-surface-500">
        Rendered by the Roku at its UI resolution.
        <span class="flex-1" />
        <Button label="Save" icon="pi pi-download" size="small" text @click="devChannel.saveScreenshot()" />
      </figcaption>
    </figure>

    <LazyRokuDevPasswordDialog v-model:visible="passwordOpen" :roku="roku" @saved="refresh" />
  </div>
</template>
