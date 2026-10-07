/** Only same-site paths are accepted as a post-login destination. */
export function safeRedirect(value: unknown): string {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//') ? value : '/'
}

export default defineNuxtRouteMiddleware(async (to) => {
  const auth = useAuthStore()
  await auth.ensureLoaded()

  if (to.meta.public) {
    if (auth.user && to.path === '/login') return navigateTo(safeRedirect(to.query.redirect))
    return
  }

  if (!auth.user) {
    return navigateTo({ path: '/login', query: to.fullPath === '/' ? {} : { redirect: to.fullPath } })
  }

  if (auth.user.mustChangePassword && to.path !== '/account') return navigateTo('/account')
  if (to.meta.requiresAdmin && !auth.isAdmin) return navigateTo('/')
})
