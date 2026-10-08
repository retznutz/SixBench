<script setup lang="ts">
import type { CaptureDevice } from '~/types/capture-device'
import type { RokuDevice } from '~/types/roku-device'

useHead({ title: 'Settings · SixBench' })

const route = useRoute()
const router = useRouter()
const devices = useCaptureDevicesStore()
const rokus = useRokuDevicesStore()
const toast = useToast()
const confirm = useConfirm()
const auth = useAuthStore()

const manualHost = ref('')
const manualPort = ref<number | null>(null)
const editing = ref<CaptureDevice | null>(null)
const dialogOpen = ref(false)

async function discover() {
  try {
    const found = await rokus.discover()
    toast.add({
      severity: found.length ? 'success' : 'info',
      summary: found.length ? `Found ${found.length} Roku${found.length === 1 ? '' : 's'}` : 'No Rokus found',
      detail: found.length ? undefined : 'Check that the Roku is on this network, or add it by IP address.',
      life: 4000,
    })
  } catch {
    // Toast already shown.
  }
}

async function addManual() {
  if (!manualHost.value.trim()) return
  try {
    const device = await rokus.addManual({ host: manualHost.value.trim(), port: manualPort.value })
    toast.add({ severity: 'success', summary: `Added ${device.friendlyName}`, life: 3000 })
    manualHost.value = ''
    manualPort.value = null
  } catch {
    // Toast already shown.
  }
}

function removeRoku(roku: RokuDevice) {
  confirm.require({
    header: 'Remove Roku?',
    message: `Remove ${roku.friendlyName}? Encoders linked to it will be unlinked.`,
    icon: 'pi pi-exclamation-triangle',
    acceptProps: { label: 'Remove', severity: 'danger' },
    rejectProps: { label: 'Cancel', severity: 'secondary', outlined: true },
    accept: async () => {
      await rokus.remove(roku.id)
      await devices.fetchAll()
    },
  })
}

function configure(device: CaptureDevice) {
  editing.value = device
  dialogOpen.value = true
}

watch(dialogOpen, (open) => {
  if (!open && route.query.device) router.replace({ query: { ...route.query, device: undefined } })
})

onMounted(async () => {
  await Promise.all([devices.fetchAll().catch(() => undefined), rokus.fetchAll().catch(() => undefined)])
  const requested = typeof route.query.device === 'string' ? devices.byId(route.query.device) : undefined
  if (requested) configure(requested)
})

function formatSeen(iso: string) {
  return new Date(iso).toLocaleString()
}
</script>

<template>
  <div class="space-y-10">
    <h1 class="text-2xl font-bold tracking-tight">Settings</h1>

    <section class="space-y-4" aria-labelledby="roku-heading">
      <div class="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 id="roku-heading" class="text-lg font-medium">Roku devices</h2>
          <p class="text-sm text-surface-400">
            Rokus must allow network control: Settings › System › Advanced system settings › Control by mobile apps.
          </p>
        </div>
        <Button label="Discover" icon="pi pi-wifi" :loading="rokus.discovering" @click="discover" />
      </div>

      <form class="flex flex-wrap items-end gap-2" @submit.prevent="addManual">
        <div class="flex min-w-48 flex-1 flex-col gap-1">
          <label for="roku-host" class="text-xs text-surface-400">IP address</label>
          <InputText id="roku-host" v-model="manualHost" placeholder="192.168.1.50" autocomplete="off" />
        </div>
        <div class="flex w-28 flex-col gap-1">
          <label for="roku-port" class="text-xs text-surface-400">Port</label>
          <InputNumber
            v-model="manualPort"
            input-id="roku-port"
            :min="1"
            :max="65535"
            :use-grouping="false"
            placeholder="8060"
            fluid
          />
        </div>
        <Button
          type="submit"
          label="Add"
          icon="pi pi-plus"
          severity="secondary"
          :loading="rokus.adding"
          :disabled="!manualHost.trim()"
        />
      </form>

      <ul v-if="rokus.devices.length" class="divide-y divide-surface-800 rounded-xl border border-surface-800">
        <li v-for="roku in rokus.devices" :key="roku.id" class="flex flex-wrap items-center gap-3 p-4">
          <i class="pi pi-desktop text-surface-500" aria-hidden="true" />
          <div class="min-w-0 flex-1">
            <p class="truncate font-medium">{{ roku.friendlyName }}</p>
            <p class="truncate text-xs text-surface-500">
              {{ roku.model ?? 'Roku' }} · {{ roku.ipAddress }}:{{ roku.port }} · {{ roku.serialNumber }} · seen
              {{ formatSeen(roku.lastSeenUtc) }}
            </p>
          </div>
          <Tag :value="roku.isManual ? 'Manual' : 'Discovered'" severity="secondary" />
          <Button
            icon="pi pi-trash"
            text
            rounded
            severity="danger"
            :aria-label="`Remove ${roku.friendlyName}`"
            @click="removeRoku(roku)"
          />
        </li>
      </ul>
      <p
        v-else-if="!rokus.loading"
        class="rounded-xl border border-dashed border-surface-800 p-6 text-center text-sm text-surface-400"
      >
        No Rokus saved yet. Click Discover or add one by IP address.
      </p>
    </section>

    <section class="space-y-4" aria-labelledby="encoder-heading">
      <div>
        <h2 id="encoder-heading" class="text-lg font-medium">Encoders</h2>
        <p class="text-sm text-surface-400">
          Link each HDMI encoder to the Roku plugged into it, and choose its audio settings.
        </p>
      </div>

      <ul v-if="devices.devices.length" class="divide-y divide-surface-800 rounded-xl border border-surface-800">
        <li v-for="device in devices.devices" :key="device.id" class="flex flex-wrap items-center gap-3 p-4">
          <i class="pi pi-video text-surface-500" aria-hidden="true" />
          <div class="min-w-0 flex-1">
            <p class="truncate font-medium">{{ devices.displayName(device) }}</p>
            <p class="truncate text-xs text-surface-500">
              {{ device.link?.rokuDevice ? `→ ${device.link.rokuDevice.friendlyName}` : 'No Roku linked' }}
              · audio {{ device.link?.allowDeviceAudio ? 'on' : 'off' }}
            </p>
          </div>
          <Tag
            :value="device.isConnected ? 'Connected' : 'Unplugged'"
            :severity="device.isConnected ? 'success' : 'secondary'"
          />
          <Button
            label="Configure"
            icon="pi pi-sliders-h"
            size="small"
            severity="secondary"
            outlined
            @click="configure(device)"
          />
        </li>
      </ul>
      <p
        v-else-if="!devices.loading"
        class="rounded-xl border border-dashed border-surface-800 p-6 text-center text-sm text-surface-400"
      >
        No capture devices detected.
      </p>
    </section>

    <section v-if="auth.isAdmin" class="space-y-4" aria-labelledby="certificate-heading">
      <div>
        <h2 id="certificate-heading" class="text-lg font-medium">Domain and Certificate Setup</h2>
        <p class="text-sm text-surface-400">
          Configure your custom domain and get a free trusted SSL certificate through DNS verification. Browsers on
          other machines only decode video over HTTPS.
        </p>
      </div>
      <LazyCertificateSetup />
    </section>

    <LazyRokuLinkDialog v-model:visible="dialogOpen" :device="editing" />
  </div>
</template>
