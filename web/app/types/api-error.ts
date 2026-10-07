/** RFC 7807 problem response returned by the API for every error. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  traceId?: string
  /** Present on validation failures: field name → messages. */
  errors?: Record<string, string[]>
}

/** Error thrown by the API layer, normalised from fetch failures and ProblemDetails bodies. */
export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail?: string
  readonly errors?: Record<string, string[]>

  constructor(status: number, title: string, detail?: string, errors?: Record<string, string[]>) {
    super(detail ?? title)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
    this.errors = errors
  }

  /** A single human-readable message including validation errors. */
  get userMessage(): string {
    const validation = this.errors ? Object.values(this.errors).flat().join(' ') : ''
    return [this.detail, validation].filter(Boolean).join(' ') || this.title
  }
}
