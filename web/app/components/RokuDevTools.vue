<script setup lang="ts">
const props = defineProps<{ rokuId: number }>()
const emit = defineEmits<{ close: [] }>()

const devTools = useRokuDevToolsStore()
const tab = ref('scenegraph')

watch(
  () => props.rokuId,
  () => devTools.reset(),
)
onBeforeUnmount(() => devTools.reset())
</script>

<template>
  <section class="rounded-xl border border-surface-800 bg-surface-900/60" aria-label="Developer tools">
    <header class="flex items-center gap-2 px-4 pt-3">
      <h2 class="text-sm font-medium text-surface-300">Developer tools</h2>
      <i
        v-tooltip.bottom="
          'Needs developer mode on the Roku and Control by mobile apps set to Enabled. Most queries only answer while a sideloaded (dev) channel is running.'
        "
        class="pi pi-info-circle text-xs text-surface-500"
        tabindex="0"
        aria-label="Requirements"
      />
      <span class="flex-1" />
      <Button
        icon="pi pi-times"
        text
        rounded
        size="small"
        severity="secondary"
        aria-label="Close developer tools"
        @click="emit('close')"
      />
    </header>

    <Tabs v-model:value="tab">
      <TabList class="px-2">
        <Tab value="scenegraph">SceneGraph</Tab>
        <Tab value="performance">Performance</Tab>
        <Tab value="registry">Registry</Tab>
      </TabList>
      <TabPanels class="!bg-transparent">
        <TabPanel value="scenegraph"><SceneGraphInspector :roku-id="rokuId" /></TabPanel>
        <TabPanel value="performance"
          ><ChannelPerformance :roku-id="rokuId" :active="tab === 'performance'"
        /></TabPanel>
        <TabPanel value="registry"><ChannelRegistry :roku-id="rokuId" /></TabPanel>
      </TabPanels>
    </Tabs>
  </section>
</template>
