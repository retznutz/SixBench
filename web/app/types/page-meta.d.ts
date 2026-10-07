declare module '#app' {
  interface PageMeta {
    /** Reachable without signing in. */
    public?: boolean
    /** Only administrators may open the page. */
    requiresAdmin?: boolean
  }
}

export {}
