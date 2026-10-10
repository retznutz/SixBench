<script setup lang="ts">
import type { RokuDevice } from '~/types/roku-device'
import { ApiError } from '~/types/api-error'

// Opened in its own browser window from the watch page, so no site navigation around it.
definePageMeta({ layout: 'popout' })

const route = useRoute()
const rokuDevices = useRokuDevicesStore()

const rokuId = computed(() => Number(route.params.id))
const initialTab = computed(() => (typeof route.query.tab === 'string' ? route.query.tab : null))
const roku = ref<RokuDevice | null>(rokuDevices.byId(rokuId.value) ?? null)
const loading = ref(false)
const error = ref<string | null>(null)

useHead({ title: () => `Dev tools · ${roku.value?.friendlyName ?? 'Roku'} · SixBench` })

async function load() {
  if (!Number.isInteger(rokuId.value) || rokuId.value <= 0) {
    error.value = 'This is not a valid Roku.'
    return
  }
  loading.value = true
  error.value = null
  try {
    roku.value = await rokuDevices.fetchOne(rokuId.value)
  } catch (e) {
    error.value = e instanceof ApiError ? e.userMessage : 'The server could not be reached.'
  } finally {
    loading.value = false
  }
}

onMounted(load)
watch(rokuId, load)
</script>

<template>
  <div class="space-y-3">
    <Message v-if="error" severity="error" :closable="false">
      <div class="flex flex-wrap items-center gap-3">
        <span>Could not load the Roku: {{ error }}</span>
        <Button label="Retry" icon="pi pi-refresh" size="small" text :loading="loading" @click="load" />
      </div>
    </Message>

    <template v-if="roku">
      <h1 class="px-1 text-base font-semibold tracking-tight">{{ roku.friendlyName }}</h1>
      <RokuDevTools
        :key="roku.id"
        :roku-id="roku.id"
        :dev-page-url="rokuDevPageUrl(roku)"
        :initial-tab="initialTab"
        popped-out
      />
    </template>
    <div v-else-if="loading" class="flex justify-center py-16">
      <ProgressSpinner style="width: 2.5rem; height: 2.5rem" stroke-width="4" aria-label="Loading" />
    </div>
  </div>
</template>
