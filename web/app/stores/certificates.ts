import { defineStore } from 'pinia'
import type {
  CertificateStatus,
  CredentialValidationResult,
  DnsProviderInfo,
  ProvisionRequest,
} from '~/types/certificate'

export const useCertificateStore = defineStore('certificates', () => {
  const api = useApi()

  const status = ref<CertificateStatus | null>(null)
  const providers = ref<DnsProviderInfo[]>([])
  const loading = ref(false)
  const provisioning = ref(false)

  async function fetchStatus() {
    loading.value = true
    try {
      status.value = await api.get<CertificateStatus>('/certificates/status')
    } finally {
      loading.value = false
    }
  }

  async function fetchProviders() {
    providers.value = await api.get<DnsProviderInfo[]>('/certificates/providers')
  }

  function validateCredentials(dnsProvider: string, dnsCredentials: Record<string, string>, domain?: string) {
    return api.post<CredentialValidationResult>('/certificates/validate-credentials', {
      body: { dnsProvider, dnsCredentials, domain },
    })
  }

  /** Detected public IP, or null if it couldn't be found. */
  async function fetchPublicIp(): Promise<string | null> {
    try {
      return (await api.get<{ ip: string }>('/certificates/public-ip', { silent: true })).ip
    } catch {
      return null
    }
  }

  /** Obtains and installs a certificate (30–90 s). Errors are not toasted; the wizard shows them. */
  async function provision(request: ProvisionRequest) {
    provisioning.value = true
    try {
      status.value = await api.post<CertificateStatus>('/certificates/provision', { body: request, silent: true })
      return status.value
    } finally {
      provisioning.value = false
    }
  }

  async function removeCertificate() {
    status.value = await api.del<CertificateStatus>('/certificates')
  }

  return {
    status,
    providers,
    loading,
    provisioning,
    fetchStatus,
    fetchProviders,
    fetchPublicIp,
    validateCredentials,
    provision,
    removeCertificate,
  }
})
