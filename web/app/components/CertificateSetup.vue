<script setup lang="ts">
import { ApiError } from '~/types/api-error'
import type { CredentialValidationResult, ProvisioningComplete, ProvisioningProgress } from '~/types/certificate'

const certStore = useCertificateStore()
const server = useServerStore()
const { build, start, stop } = useSignalR('/hubs/certificates')
const toast = useToast()
const confirm = useConfirm()

const activeStep = ref<string | number>(1)
const selectedProvider = ref('')
const credentials = ref<Record<string, string>>({})
const subdomain = ref('sixbench')
const rootDomain = ref('')
const email = ref('')
const setupDnsRecord = ref(true)
const publicIp = ref('')
const detectingIp = ref(false)
const showPortForwarding = ref(false)
const validating = ref(false)
const validationResult = ref<CredentialValidationResult | null>(null)
const provisioningSteps = ref<ProvisioningProgress[]>([])
const provisioningComplete = ref<ProvisioningComplete | null>(null)

const serverPort = computed(() => certStore.status?.port ?? 5216)

const credentialLabels: Record<string, Record<string, string>> = {
  Cloudflare: { ApiToken: 'API Token' },
  DuckDNS: { Token: 'Token', Subdomain: 'Subdomain (without .duckdns.org)' },
  Route53: { AccessKeyId: 'Access Key ID', SecretAccessKey: 'Secret Access Key' },
  DigitalOcean: { ApiToken: 'API Token' },
  GoDaddy: { ApiKey: 'API Key', ApiSecret: 'API Secret' },
}

const credentialHelp: Record<string, string> = {
  Cloudflare: 'Create an API token at dash.cloudflare.com with DNS:Edit permission for your zone.',
  DuckDNS: 'Get your token from duckdns.org after logging in, and enter your DuckDNS subdomain.',
  Route53: 'Create an IAM user with Route 53 ChangeResourceRecordSets and ListHostedZonesByName permissions.',
  DigitalOcean: 'Generate a personal access token with write scope at cloud.digitalocean.com/account/api/tokens.',
  GoDaddy: 'Create API keys at developer.godaddy.com. Use the Production environment keys.',
}

const isDuckDns = computed(() => selectedProvider.value === 'DuckDNS')

const fullDomain = computed(() => {
  if (isDuckDns.value) {
    const sub = (credentials.value.Subdomain ?? '').replace(/\.duckdns\.org$/i, '').trim()
    return sub ? `${sub}.duckdns.org` : ''
  }
  return rootDomain.value.trim() && subdomain.value.trim()
    ? `${subdomain.value.trim()}.${rootDomain.value.trim()}`.toLowerCase()
    : ''
})

const selectedProviderInfo = computed(() => certStore.providers.find((p) => p.name === selectedProvider.value))
const requiredKeys = computed(() => selectedProviderInfo.value?.requiredCredentials ?? [])

const canProceedToCredentials = computed(() => !!selectedProvider.value)
const canProceedToProvision = computed(() => {
  if (!fullDomain.value || !email.value.trim() || !selectedProvider.value) return false
  if (setupDnsRecord.value && !publicIp.value.trim()) return false
  return requiredKeys.value.every((k) => !!credentials.value[k]?.trim())
})

function isSecret(key: string) {
  const k = key.toLowerCase()
  return k.includes('secret') || k.includes('token') || k.includes('key')
}

function onProviderSelect(name: string) {
  selectedProvider.value = name
  credentials.value = {}
  validationResult.value = null
}

async function detectPublicIp() {
  detectingIp.value = true
  const ip = await certStore.fetchPublicIp()
  if (ip) publicIp.value = ip
  detectingIp.value = false
}

async function testCredentials() {
  validating.value = true
  validationResult.value = null
  try {
    validationResult.value = await certStore.validateCredentials(
      selectedProvider.value,
      credentials.value,
      fullDomain.value || undefined,
    )
  } catch (e) {
    validationResult.value = { success: false, message: e instanceof ApiError ? e.userMessage : 'Validation failed' }
  } finally {
    validating.value = false
  }
}

async function startProvisioning() {
  provisioningSteps.value = []
  provisioningComplete.value = null
  activeStep.value = 3

  // Progress over SignalR is a nicety; the HTTP response below is what counts.
  const connection = build()
  connection.off('CertificateProvisioningProgress')
  connection.off('CertificateProvisioningComplete')
  connection.on('CertificateProvisioningProgress', (data: ProvisioningProgress) => {
    if (data.step !== 'Failed') provisioningSteps.value.push(data)
  })
  connection.on('CertificateProvisioningComplete', (data: ProvisioningComplete) => {
    provisioningComplete.value ??= data
  })
  await start()

  try {
    await certStore.provision({
      domain: fullDomain.value,
      email: email.value.trim(),
      dnsProvider: selectedProvider.value,
      dnsCredentials: credentials.value,
      setupDnsRecord: setupDnsRecord.value,
      publicIp: setupDnsRecord.value ? publicIp.value.trim() || null : null,
    })
    provisioningComplete.value = { success: true, message: 'Certificate provisioned successfully!' }
  } catch (e) {
    provisioningComplete.value = {
      success: false,
      message: e instanceof ApiError ? e.userMessage : 'Provisioning failed',
    }
  }
}

async function restartServer() {
  try {
    const relaunched = await server.restart()
    const url = certStore.status?.httpsUrl
    toast.add({
      severity: 'info',
      summary: 'Server is restarting',
      detail: relaunched
        ? `Reconnect at ${url ?? 'the new HTTPS address'} in a few seconds.`
        : 'SixBench is stopping; your service manager should start it again.',
      life: 15000,
    })
  } catch {
    // Toast already shown.
  }
}

function resetWizard() {
  activeStep.value = 1
  selectedProvider.value = ''
  credentials.value = {}
  subdomain.value = 'sixbench'
  rootDomain.value = ''
  email.value = ''
  setupDnsRecord.value = true
  validationResult.value = null
  provisioningSteps.value = []
  provisioningComplete.value = null
  void detectPublicIp()
}

function removeCert() {
  confirm.require({
    header: 'Remove certificate?',
    message: 'SixBench keeps serving HTTPS until it restarts, then switches back to HTTP.',
    icon: 'pi pi-exclamation-triangle',
    acceptProps: { label: 'Remove', severity: 'danger' },
    rejectProps: { label: 'Cancel', severity: 'secondary', outlined: true },
    accept: () => certStore.removeCertificate(),
  })
}

async function copyUrl() {
  const url = certStore.status?.httpsUrl
  if (!url) return
  try {
    await navigator.clipboard.writeText(url)
    toast.add({ severity: 'info', summary: 'Copied', life: 1500 })
  } catch {
    toast.add({
      severity: 'warn',
      summary: 'Copy failed',
      detail: 'Select the address and copy it instead.',
      life: 4000,
    })
  }
}

function stepLabel(step: string) {
  return step.replace(/([A-Z])/g, ' $1').trim()
}

onMounted(async () => {
  await Promise.all([certStore.fetchStatus().catch(() => undefined), certStore.fetchProviders().catch(() => undefined)])
  void detectPublicIp()
})

onUnmounted(() => void stop())
</script>

<template>
  <div>
    <div v-if="certStore.loading && !certStore.status" class="flex items-center gap-2 p-4 text-sm text-zinc-400">
      <ProgressSpinner style="width: 1.25rem; height: 1.25rem" stroke-width="6" />
      <span>Loading certificate status…</span>
    </div>

    <!-- Current certificate -->
    <div
      v-else-if="certStore.status?.hasCertificate && activeStep === 1"
      class="flex flex-wrap items-start gap-3 rounded-xl border border-primary/40 bg-primary/5 p-4"
    >
      <i class="pi pi-lock mt-1 text-2xl text-primary" aria-hidden="true" />
      <div class="min-w-0 flex-1">
        <h3 class="text-lg font-semibold text-primary">HTTPS and custom domain active</h3>
        <div class="mt-2 flex items-center gap-2">
          <code class="truncate rounded bg-primary/10 px-2 py-1 text-sm font-medium text-primary">{{
            certStore.status.httpsUrl
          }}</code>
          <Button icon="pi pi-copy" text rounded size="small" aria-label="Copy address" @click="copyUrl" />
        </div>
        <p class="mt-2 text-sm text-zinc-400">
          Valid until {{ new Date(certStore.status.info?.notAfter ?? '').toLocaleDateString() }} · Issuer:
          {{ certStore.status.info?.issuer }}
        </p>
        <p class="mt-1 text-xs text-zinc-500">
          <span v-if="certStore.status.autoRenewEnabled"
            >Automatic renewal is enabled. DNS credentials are encrypted locally and used only for renewal.</span
          >
          <span v-else
            >Automatic renewal is not fully configured. Set up the domain again to store encrypted DNS
            credentials.</span
          >
        </p>
        <div v-if="!certStore.status.httpsActive" class="mt-3 flex flex-wrap items-center gap-3">
          <Message severity="warn" :closable="false" class="flex-1">
            <span class="text-sm">A server restart is required to switch port {{ serverPort }} to HTTPS.</span>
          </Message>
          <Button
            label="Restart Now"
            icon="pi pi-sync"
            severity="danger"
            size="small"
            :loading="server.restarting"
            @click="restartServer"
          />
        </div>
      </div>
      <div class="flex gap-2">
        <Button label="Set up again" severity="secondary" size="small" outlined @click="activeStep = 0" />
        <Button label="Remove" severity="danger" size="small" outlined @click="removeCert" />
      </div>
    </div>

    <!-- Setup wizard -->
    <Stepper v-else :value="activeStep === 0 ? 1 : activeStep" linear @update:value="activeStep = $event">
      <StepList>
        <Step :value="1">DNS Provider</Step>
        <Step :value="2">Domain &amp; Credentials</Step>
        <Step :value="3">Provision</Step>
      </StepList>

      <StepPanels>
        <!-- Step 1: provider -->
        <StepPanel :value="1">
          <div class="py-4">
            <div class="mb-4 grid grid-cols-1 gap-3 md:grid-cols-2 lg:grid-cols-3">
              <button
                v-for="provider in certStore.providers"
                :key="provider.name"
                type="button"
                class="rounded-xl border-2 p-4 text-left transition-all"
                :class="
                  selectedProvider === provider.name
                    ? 'border-primary bg-primary/5 ring-2 ring-primary/30'
                    : 'border-zinc-800 hover:border-zinc-600'
                "
                :aria-pressed="selectedProvider === provider.name"
                @click="onProviderSelect(provider.name)"
              >
                <div class="font-semibold" :class="{ 'text-primary': selectedProvider === provider.name }">
                  {{ provider.name }}
                </div>
                <div class="mt-1 text-xs text-zinc-500">Requires: {{ provider.requiredCredentials.join(', ') }}</div>
              </button>
            </div>
            <div class="flex justify-end">
              <Button label="Next" :disabled="!canProceedToCredentials" @click="activeStep = 2" />
            </div>
          </div>
        </StepPanel>

        <!-- Step 2: domain and credentials -->
        <StepPanel :value="2">
          <div class="py-4">
            <div class="grid grid-cols-1 gap-8 lg:grid-cols-2">
              <div>
                <Message v-if="credentialHelp[selectedProvider]" severity="info" :closable="false" class="mb-4">
                  {{ credentialHelp[selectedProvider] }}
                </Message>

                <div class="flex flex-col gap-4">
                  <div class="flex flex-col gap-1">
                    <label for="cert-subdomain" class="text-sm font-medium">Domain name</label>
                    <div v-if="!isDuckDns" class="flex items-center gap-1">
                      <InputText id="cert-subdomain" v-model="subdomain" placeholder="sixbench" class="w-28" />
                      <span class="text-lg font-light text-zinc-500">.</span>
                      <InputText
                        v-model="rootDomain"
                        placeholder="mydomain.com"
                        class="min-w-0 flex-1"
                        aria-label="Root domain"
                        autocapitalize="off"
                        spellcheck="false"
                      />
                    </div>
                    <p v-else class="text-sm text-zinc-400">
                      {{ fullDomain || 'Enter your DuckDNS subdomain below' }}
                    </p>
                    <small v-if="fullDomain" class="text-zinc-400">
                      Your server will be accessible at
                      <strong class="text-zinc-200">https://{{ fullDomain }}:{{ serverPort }}</strong>
                    </small>
                  </div>

                  <div class="flex flex-col gap-1">
                    <label for="cert-email" class="text-sm font-medium">Email</label>
                    <InputText id="cert-email" v-model="email" placeholder="you@example.com" type="email" />
                    <small class="text-zinc-400">Used for your Let's Encrypt account.</small>
                  </div>

                  <Divider />

                  <div v-for="(key, index) in requiredKeys" :key="key" class="flex flex-col gap-1">
                    <label :for="`cert-cred-${key}`" class="text-sm font-medium">
                      {{ credentialLabels[selectedProvider]?.[key] || key }}
                    </label>
                    <InputGroup>
                      <Password
                        v-if="isSecret(key)"
                        v-model="credentials[key]"
                        :input-id="`cert-cred-${key}`"
                        :feedback="false"
                        toggle-mask
                        fluid
                        :input-props="{ autocomplete: 'off' }"
                      />
                      <InputText v-else :id="`cert-cred-${key}`" v-model="credentials[key]" autocomplete="off" />
                      <Button
                        v-if="index === requiredKeys.length - 1"
                        label="Test"
                        severity="secondary"
                        :loading="validating"
                        @click="testCredentials"
                      />
                    </InputGroup>
                    <div v-if="index === requiredKeys.length - 1 && validationResult" class="mt-1">
                      <Tag v-if="validationResult.success" severity="success" value="Valid" />
                      <Tag v-else severity="danger" :value="validationResult.message" class="whitespace-normal" />
                    </div>
                  </div>
                </div>
              </div>

              <div class="flex flex-col gap-4">
                <div class="flex items-start gap-3">
                  <ToggleSwitch v-model="setupDnsRecord" input-id="cert-a-record" class="mt-0.5 shrink-0" />
                  <div>
                    <label for="cert-a-record" class="text-sm font-medium">Point domain to this server</label>
                    <p class="mt-0.5 text-xs text-zinc-400">
                      Automatically create a DNS A record so <strong>{{ fullDomain || 'your domain' }}</strong> resolves
                      to your server's public IP address.
                    </p>
                  </div>
                </div>

                <div v-if="setupDnsRecord" class="ml-12 flex flex-col gap-1">
                  <label for="cert-public-ip" class="text-sm font-medium">Public IP address</label>
                  <InputGroup>
                    <InputText id="cert-public-ip" v-model="publicIp" placeholder="203.0.113.42" />
                    <Button label="Detect" severity="secondary" :loading="detectingIp" @click="detectPublicIp" />
                  </InputGroup>
                  <small class="text-zinc-400">Your server's public IP (detected when this page opens)</small>
                </div>

                <Button
                  label="Port Forwarding Instructions"
                  icon="pi pi-info-circle"
                  severity="secondary"
                  outlined
                  size="small"
                  class="self-start"
                  @click="showPortForwarding = true"
                />
              </div>
            </div>

            <div class="mt-6 flex justify-between gap-2">
              <Button label="Back" severity="secondary" @click="activeStep = 1" />
              <Button
                label="Next: Provision Certificate"
                icon="pi pi-lock"
                icon-pos="right"
                :disabled="!canProceedToProvision"
                @click="startProvisioning"
              />
            </div>
          </div>
        </StepPanel>

        <!-- Step 3: progress -->
        <StepPanel :value="3">
          <div class="py-4">
            <div class="flex flex-col gap-3">
              <div
                v-for="(s, i) in provisioningSteps"
                :key="i"
                class="flex items-start gap-3 rounded-md p-3"
                :class="i === provisioningSteps.length - 1 && !provisioningComplete ? 'bg-primary/10' : 'bg-zinc-900'"
              >
                <ProgressSpinner
                  v-if="i === provisioningSteps.length - 1 && !provisioningComplete"
                  style="width: 1rem; height: 1rem"
                  stroke-width="8"
                  class="!m-0 mt-0.5"
                />
                <i v-else class="pi pi-check-circle mt-0.5 text-green-400" aria-hidden="true" />
                <div>
                  <div class="text-sm font-medium">{{ stepLabel(s.step) }}</div>
                  <div class="text-xs text-zinc-400">{{ s.message }}</div>
                </div>
              </div>

              <div
                v-if="provisioningSteps.length === 0 && !provisioningComplete"
                class="flex min-h-64 flex-col items-center justify-center gap-4 py-12 text-center"
                role="status"
              >
                <ProgressSpinner style="width: 3rem; height: 3rem" stroke-width="4" />
                <span class="text-lg font-medium text-zinc-300">Starting certificate provisioning…</span>
                <p class="max-w-md text-sm text-zinc-400">
                  Connecting to Let's Encrypt, setting DNS records, and validating domain ownership. This typically
                  takes 30–90 seconds.
                </p>
              </div>
            </div>

            <div
              v-if="provisioningComplete"
              class="flex min-h-64 flex-col items-center justify-center gap-4 py-12 text-center"
            >
              <Message v-if="provisioningComplete.success" severity="success" :closable="false" class="max-w-md">
                <div class="font-semibold">Certificate provisioned successfully!</div>
                <div class="mt-1 text-sm">
                  Your server certificate is installed. DNS credentials were encrypted locally and the certificate will
                  auto-renew 30 days before expiry.
                </div>
              </Message>
              <Message v-else severity="error" :closable="false" class="max-w-md">
                <div class="font-semibold">Provisioning failed</div>
                <div class="mt-1 text-sm">{{ provisioningComplete.message }}</div>
              </Message>

              <div v-if="provisioningComplete.success" class="flex flex-col items-center gap-3">
                <Message severity="warn" :closable="false" class="max-w-md">
                  <div class="text-sm">A server restart is required to bind to HTTPS with the new certificate.</div>
                </Message>
                <div class="flex gap-3">
                  <Button
                    label="Restart Now"
                    severity="danger"
                    icon="pi pi-sync"
                    :loading="server.restarting"
                    @click="restartServer"
                  />
                  <Button label="Done" severity="secondary" @click="resetWizard" />
                </div>
              </div>
              <Button v-else label="Try Again" severity="secondary" @click="activeStep = 2" />
            </div>
          </div>
        </StepPanel>
      </StepPanels>
    </Stepper>

    <Dialog
      v-model:visible="showPortForwarding"
      header="Port Forwarding Instructions"
      modal
      class="w-[min(32rem,calc(100vw-2rem))]"
    >
      <div class="space-y-4 text-sm">
        <p class="text-zinc-400">
          For remote access to work, your router must forward incoming traffic on port
          <strong class="text-zinc-200">{{ serverPort }}</strong> to this server.
        </p>

        <div class="space-y-2 rounded-md bg-zinc-900 p-4">
          <h4 class="font-semibold">Steps:</h4>
          <ol class="list-inside list-decimal space-y-2 text-zinc-400">
            <li>
              Log into your router's admin page (usually <code class="rounded bg-zinc-800 px-1">192.168.1.1</code> or
              <code class="rounded bg-zinc-800 px-1">192.168.0.1</code>)
            </li>
            <li>Find <strong>Port Forwarding</strong> (may be under NAT, Firewall, or Advanced)</li>
            <li>
              Create a new port forwarding rule:
              <div class="ml-4 mt-2 space-y-1.5 rounded border border-zinc-800 bg-zinc-950 p-3">
                <div class="flex justify-between">
                  <span>External Port</span><strong>{{ serverPort }}</strong>
                </div>
                <div class="flex justify-between"><span>Internal IP</span><strong>This server's LAN IP</strong></div>
                <div class="flex justify-between">
                  <span>Internal Port</span><strong>{{ serverPort }}</strong>
                </div>
                <div class="flex justify-between"><span>Protocol</span><strong>TCP</strong></div>
              </div>
            </li>
            <li>Save and apply the rule</li>
          </ol>
        </div>

        <Message severity="warn" :closable="false">
          <strong>ISP restrictions:</strong> Some ISPs use CGNAT (Carrier-Grade NAT), which blocks incoming connections.
          If port forwarding doesn't work, ask your ISP for a public IP, or use a tunnel service such as Cloudflare
          Tunnel or Tailscale.
        </Message>

        <Message severity="warn" :closable="false">
          <strong>Security:</strong> forwarding the port makes SixBench reachable from the internet. Use strong
          passwords for every account.
        </Message>

        <Message severity="info" :closable="false">
          <strong>Note:</strong> the certificate is issued regardless of port forwarding; it only needs DNS access. Port
          forwarding is only needed for viewers outside your network.
        </Message>
      </div>

      <template #footer>
        <Button label="Got it" @click="showPortForwarding = false" />
      </template>
    </Dialog>
  </div>
</template>
