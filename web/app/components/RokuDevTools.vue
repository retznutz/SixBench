<script setup lang="ts">
const props = defineProps<{
  rokuId: number
  devPageUrl?: string | null
  /** Shown in its own browser window: no pop-out or close button (the window has its own). */
  poppedOut?: boolean
  /** Tab to start on; ignored if unknown or not available to this user. */
  initialTab?: string | null
}>()
const emit = defineEmits<{ close: []; popOut: [tab: string] }>()

const devTools = useRokuDevToolsStore()
const devChannel = useRokuDevChannelStore()
const auth = useAuthStore()

const ADMIN_TABS = ['console', 'packager']
const TABS = ['channel', 'scenegraph', 'performance', 'registry', ...ADMIN_TABS]
const tab = ref(
  props.initialTab && TABS.includes(props.initialTab) && (auth.isAdmin || !ADMIN_TABS.includes(props.initialTab))
    ? props.initialTab
    : 'channel',
)

watch(
  () => props.rokuId,
  () => {
    devTools.reset()
    devChannel.reset()
  },
)
onBeforeUnmount(() => {
  devTools.reset()
  devChannel.reset()
})
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
        v-if="devPageUrl"
        as="a"
        :href="devPageUrl"
        target="_blank"
        rel="noopener noreferrer"
        label="Roku dev page"
        icon="pi pi-external-link"
        icon-pos="right"
        text
        size="small"
        severity="secondary"
      />
      <template v-if="!poppedOut">
        <Button
          v-tooltip.bottom="'Open in a new window'"
          icon="pi pi-window-maximize"
          text
          rounded
          size="small"
          severity="secondary"
          aria-label="Open developer tools in a new window"
          @click="emit('popOut', tab)"
        />
        <Button
          icon="pi pi-times"
          text
          rounded
          size="small"
          severity="secondary"
          aria-label="Close developer tools"
          @click="emit('close')"
        />
      </template>
    </header>

    <Tabs v-model:value="tab">
      <TabList class="px-2">
        <Tab value="channel">Channel</Tab>
        <Tab v-if="auth.isAdmin" value="console">Console</Tab>
        <Tab value="scenegraph">SceneGraph</Tab>
        <Tab value="performance">Performance</Tab>
        <Tab value="registry">Registry</Tab>
        <Tab v-if="auth.isAdmin" value="packager">Packager</Tab>
      </TabList>
      <TabPanels class="!bg-transparent">
        <TabPanel value="channel"><RokuDevChannel :roku-id="rokuId" /></TabPanel>
        <TabPanel v-if="auth.isAdmin" value="console"><RokuDebugConsole :roku-id="rokuId" /></TabPanel>
        <TabPanel value="scenegraph"><SceneGraphInspector :roku-id="rokuId" /></TabPanel>
        <TabPanel value="performance"
          ><ChannelPerformance :roku-id="rokuId" :active="tab === 'performance'"
        /></TabPanel>
        <TabPanel value="registry"><ChannelRegistry :roku-id="rokuId" /></TabPanel>
        <TabPanel v-if="auth.isAdmin" value="packager"><RokuDevPackager :roku-id="rokuId" /></TabPanel>
      </TabPanels>
    </Tabs>
  </section>
</template>
