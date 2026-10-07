<script setup lang="ts">
const route = useRoute()
const auth = useAuthStore()

const links = computed(() => [
  { to: '/', label: 'Encoders', icon: 'pi pi-video' },
  { to: '/settings', label: 'Settings', icon: 'pi pi-cog' },
  ...(auth.isAdmin ? [{ to: '/users', label: 'Users', icon: 'pi pi-users' }] : []),
])

async function signOut() {
  await auth.logout()
  await navigateTo('/login')
}

function isActive(to: string) {
  return to === '/' ? route.path === '/' || route.path.startsWith('/watch') : route.path.startsWith(to)
}
</script>

<template>
  <div class="flex min-h-dvh flex-col">
    <header class="sticky top-0 z-20 border-b border-zinc-800 bg-zinc-950/90 backdrop-blur">
      <div class="mx-auto flex h-14 max-w-7xl items-center gap-4 px-4">
        <NuxtLink to="/" class="flex items-center gap-2 font-semibold tracking-tight text-zinc-100">
          <span class="grid size-8 place-items-center rounded-lg bg-primary text-primary-contrast">
            <i class="pi pi-desktop text-sm" aria-hidden="true" />
          </span>
          SixBench
        </NuxtLink>
        <nav class="ml-auto flex items-center gap-1" aria-label="Main">
          <NuxtLink
            v-for="link in links"
            :key="link.to"
            :to="link.to"
            class="flex items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors"
            :class="
              isActive(link.to) ? 'bg-zinc-800 text-zinc-50' : 'text-zinc-400 hover:bg-zinc-900 hover:text-zinc-100'
            "
            :aria-current="isActive(link.to) ? 'page' : undefined"
          >
            <i :class="link.icon" aria-hidden="true" />
            <span class="hidden sm:inline">{{ link.label }}</span>
          </NuxtLink>
          <NuxtLink
            to="/account"
            class="flex items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors"
            :class="
              isActive('/account') ? 'bg-zinc-800 text-zinc-50' : 'text-zinc-400 hover:bg-zinc-900 hover:text-zinc-100'
            "
            :aria-current="isActive('/account') ? 'page' : undefined"
            :title="`Signed in as ${auth.user?.userName}`"
          >
            <i class="pi pi-user" aria-hidden="true" />
            <span class="hidden max-w-32 truncate md:inline">{{ auth.user?.userName }}</span>
          </NuxtLink>
          <Button
            icon="pi pi-sign-out"
            text
            rounded
            severity="secondary"
            aria-label="Sign out"
            title="Sign out"
            @click="signOut"
          />
        </nav>
      </div>
    </header>
    <main class="mx-auto w-full max-w-7xl flex-1 px-4 py-6">
      <slot />
    </main>
  </div>
</template>
