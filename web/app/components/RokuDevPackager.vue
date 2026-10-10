<script setup lang="ts">
/** Packager (signed .pkg of the sideloaded channel) and Rekey. Admin only. */
const props = defineProps<{ rokuId: number }>()

const devChannel = useRokuDevChannelStore()
const confirm = useConfirm()

const form = reactive({ appName: '', version: '1.0', signingPassword: '' })
const rekeyFile = ref<File | null>(null)
const rekeyPassword = ref('')
const rekeyInput = ref<HTMLInputElement | null>(null)

const status = computed(() => devChannel.status)
const busy = computed(() => devChannel.busy != null)
const versionValid = computed(() => /^\d+(\.\d+){0,3}$/.test(form.version.trim()))

// Default the package name to the sideloaded channel's.
watch(
  () => status.value?.devChannel,
  (channel) => {
    if (!channel) return
    if (!form.appName) form.appName = channel.name
    if (channel.version && form.version === '1.0') form.version = channel.version
  },
  { immediate: true },
)

async function pack() {
  const ok = await devChannel.packageChannel(props.rokuId, {
    appName: form.appName.trim(),
    version: form.version.trim(),
    signingPassword: form.signingPassword,
  })
  if (ok) form.signingPassword = ''
}

function rekey() {
  const file = rekeyFile.value
  if (!file) return
  confirm.require({
    header: 'Rekey this Roku?',
    message:
      "This replaces the Roku's signing key with the one in the package. Packages signed with the current key can only be updated from a Roku that still has it.",
    icon: 'pi pi-exclamation-triangle',
    acceptProps: { label: 'Rekey', severity: 'danger' },
    rejectProps: { label: 'Cancel', severity: 'secondary', outlined: true },
    accept: async () => {
      const result = await devChannel.rekey(props.rokuId, file, rekeyPassword.value)
      if (result) {
        rekeyPassword.value = ''
        rekeyFile.value = null
        if (rekeyInput.value) rekeyInput.value.value = ''
      }
    },
  })
}
</script>

<template>
  <div class="space-y-5">
    <Message v-if="status && !status.hasDevPassword" severity="secondary" size="small">
      Save this Roku's developer password (Settings › Rokus, or the Channel tab) to use the packager.
    </Message>

    <section class="space-y-3" aria-labelledby="packager-heading">
      <div>
        <h3 id="packager-heading" class="text-xs font-medium uppercase tracking-wide text-surface-500">
          Package the sideloaded channel
        </h3>
        <p class="mt-1 text-xs text-surface-500">
          Signs the installed dev channel with this Roku's developer key and downloads the
          <span class="font-mono">.pkg</span> for the Roku channel store.
        </p>
      </div>

      <Message v-if="status && !status.keyedDeveloperId" severity="warn" size="small">
        This Roku has no signing key yet. Generate one with <span class="font-mono">genkey</span> over telnet (port
        8080), or rekey it below from a package you signed before.
      </Message>
      <p v-else-if="status?.keyedDeveloperId" class="text-xs text-surface-500">
        Signing key <span class="font-mono text-surface-300">{{ status.keyedDeveloperId }}</span>
      </p>

      <form class="grid gap-3 sm:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]" @submit.prevent="pack">
        <div class="flex flex-col gap-1">
          <label for="pkg-name" class="text-xs text-surface-400">Channel name</label>
          <InputText id="pkg-name" v-model="form.appName" size="small" maxlength="100" required />
        </div>
        <div class="flex flex-col gap-1">
          <label for="pkg-version" class="text-xs text-surface-400">Version</label>
          <InputText
            id="pkg-version"
            v-model="form.version"
            size="small"
            maxlength="32"
            placeholder="1.0"
            :invalid="!!form.version && !versionValid"
            required
          />
        </div>
        <div class="flex flex-col gap-1 sm:col-span-2">
          <label for="pkg-password" class="text-xs text-surface-400">Signing password (from genkey)</label>
          <Password
            v-model="form.signingPassword"
            input-id="pkg-password"
            :feedback="false"
            toggle-mask
            fluid
            size="small"
            :input-props="{ autocomplete: 'off', required: true }"
          />
          <small class="text-surface-500">Sent to the Roku for this package only; SixBench never stores it.</small>
        </div>
        <div class="sm:col-span-2">
          <Button
            type="submit"
            label="Package and download"
            icon="pi pi-download"
            size="small"
            :loading="devChannel.busy === 'package'"
            :disabled="
              busy ||
              !status?.hasDevPassword ||
              !status?.devChannel ||
              !form.appName.trim() ||
              !versionValid ||
              !form.signingPassword
            "
          />
        </div>
      </form>
      <RokuDevResult :actions="['package']" />
    </section>

    <section class="space-y-3 border-t border-surface-800 pt-4" aria-labelledby="rekey-heading">
      <div>
        <h3 id="rekey-heading" class="text-xs font-medium uppercase tracking-wide text-surface-500">Rekey</h3>
        <p class="mt-1 text-xs text-surface-500">
          Installs the signing key from a package you signed before, so this Roku can sign updates to that channel.
        </p>
      </div>
      <form class="flex flex-col gap-3" @submit.prevent="rekey">
        <div class="flex flex-wrap items-center gap-2">
          <input
            ref="rekeyInput"
            type="file"
            accept=".pkg"
            class="hidden"
            @change="rekeyFile = ($event.target as HTMLInputElement).files?.[0] ?? null"
          />
          <Button
            label="Choose .pkg"
            icon="pi pi-folder-open"
            size="small"
            severity="secondary"
            outlined
            @click="rekeyInput?.click()"
          />
          <span class="min-w-0 truncate text-sm" :class="rekeyFile ? '' : 'text-surface-500'">
            {{ rekeyFile?.name ?? 'No package chosen' }}
          </span>
        </div>
        <div class="flex flex-col gap-1">
          <label for="rekey-password" class="text-xs text-surface-400">That package's signing password</label>
          <Password
            v-model="rekeyPassword"
            input-id="rekey-password"
            :feedback="false"
            toggle-mask
            fluid
            size="small"
            :input-props="{ autocomplete: 'off', required: true }"
          />
        </div>
        <div>
          <Button
            type="submit"
            label="Rekey"
            icon="pi pi-key"
            size="small"
            severity="danger"
            outlined
            :loading="devChannel.busy === 'rekey'"
            :disabled="busy || !status?.hasDevPassword || !rekeyFile || !rekeyPassword"
          />
        </div>
      </form>
      <RokuDevResult :actions="['rekey']" />
    </section>
  </div>
</template>
