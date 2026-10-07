/** A supported DNS provider (GET /certificates/providers). */
export interface DnsProviderInfo {
  name: string
  requiredCredentials: string[]
}

/** The installed certificate. */
export interface CertificateInfo {
  domain: string
  notBefore: string
  notAfter: string
  issuer: string
  thumbprint: string
}

/** Certificate state (GET /certificates/status). */
export interface CertificateStatus {
  hasCertificate: boolean
  info: CertificateInfo | null
  autoRenewEnabled: boolean
  /** The server is serving HTTPS now (false until restarted after the first certificate). */
  httpsActive: boolean
  httpsUrl: string | null
  port: number
}

/** Result of POST /certificates/validate-credentials. */
export interface CredentialValidationResult {
  success: boolean
  message: string
}

/** Body for POST /certificates/provision. */
export interface ProvisionRequest {
  domain: string
  email: string
  dnsProvider: string
  dnsCredentials: Record<string, string>
  setupDnsRecord: boolean
  publicIp: string | null
}

/** Pushed on /hubs/certificates while provisioning. */
export interface ProvisioningProgress {
  step: string
  message: string
}

/** Pushed on /hubs/certificates when provisioning ends. */
export interface ProvisioningComplete {
  success: boolean
  message: string
}
