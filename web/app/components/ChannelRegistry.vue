<script setup lang="ts">
const props = defineProps<{ rokuId: number }>()

const devTools = useRokuDevToolsStore()
const auth = useAuthStore()

const appId = ref('dev')

const itemCount = computed(() => devTools.registry?.sections.reduce((n, s) => n + s.items.length, 0) ?? 0)

function load() {
  const id = appId.value.trim()
  if (id) devTools.loadRegistry(props.rokuId, id)
}
</script>

<template>
  <div class="space-y-3">
    <Message v-if="!auth.isAdmin" severity="secondary" size="small">
      Only administrators can read channel registries, since they often hold account ids and tokens.
    </Message>

    <template v-else>
      <form class="flex flex-wrap items-center gap-2" @submit.prevent="load">
        <label for="registry-app-id" class="text-xs text-surface-400">Channel id</label>
        <InputText id="registry-app-id" v-model="appId" size="small" class="w-40 font-mono" placeholder="dev" />
        <Button
          type="submit"
          label="Read registry"
          icon="pi pi-database"
          size="small"
          :loading="devTools.registryLoading"
          :disabled="!appId.trim()"
        />
      </form>
      <p class="text-xs text-surface-500">
        Use <span class="font-mono">dev</span> for the sideloaded channel. Store channels only work when they're linked
        to the same developer account as this Roku.
      </p>

      <Message v-if="devTools.registryError" severity="warn" size="small">
        <strong>{{ devTools.registryError.title }}.</strong> {{ devTools.registryError.userMessage }}
      </Message>

      <template v-if="devTools.registry">
        <p class="text-xs text-surface-500">
          <span class="font-mono text-surface-300">{{ devTools.registry.appId }}</span>
          · {{ devTools.registry.sections.length }} sections · {{ itemCount }} entries
          <template v-if="devTools.registry.spaceAvailableBytes != null">
            · {{ devTools.registry.spaceAvailableBytes.toLocaleString() }} bytes free
          </template>
          <template v-if="devTools.registry.devId">
            · dev id <span class="font-mono">{{ devTools.registry.devId }}</span>
          </template>
        </p>

        <p v-if="!devTools.registry.sections.length" class="text-sm text-surface-500">The registry is empty.</p>

        <Panel
          v-for="section in devTools.registry.sections"
          :key="section.name"
          :header="`${section.name} (${section.items.length})`"
          toggleable
          class="text-sm"
        >
          <dl class="grid grid-cols-[minmax(0,1fr)_minmax(0,2fr)] gap-x-4 gap-y-1.5 text-xs">
            <template v-for="item in section.items" :key="item.key">
              <dt class="break-all font-mono text-surface-400">{{ item.key }}</dt>
              <dd class="max-h-32 overflow-auto whitespace-pre-wrap break-all font-mono text-surface-200">
                {{ item.value }}
              </dd>
            </template>
          </dl>
          <p v-if="!section.items.length" class="text-xs text-surface-500">No entries.</p>
        </Panel>
      </template>
    </template>
  </div>
</template>
