<script setup lang="ts">
import type { TreeNode } from 'primevue/treenode'
import type { SgNode, SgNodeScope } from '~/types/roku-dev-tools'
import { downloadBlob, snapshotFileName } from '~/lib/stream/snapshot'

const props = defineProps<{ rokuId: number }>()

const devTools = useRokuDevToolsStore()

const scope = ref<SgNodeScope>('All')
const nodeId = ref('')
const sizes = ref(false)
const filter = ref('')
const expandedKeys = ref<Record<string, boolean>>({})
const selectionKeys = ref<Record<string, boolean>>({})

const scopeOptions = [
  { label: 'All nodes', value: 'All' },
  { label: 'Roots', value: 'Roots' },
  { label: 'By id', value: 'Nodes' },
]

/** Attributes shown first in the details pane, in this order. */
const KEY_ATTRIBUTES = ['name', 'extends', 'focused', 'visible', 'opacity', 'bounds', 'translation', 'uri', 'text']

const canLoad = computed(() => scope.value !== 'Nodes' || nodeId.value.trim().length > 0)

function label(node: SgNode): string {
  const name = node.attributes.name
  return name ? `${node.type} #${name}` : node.type
}

/** Maps nodes to PrimeVue tree nodes; keys are index paths ("0-3-1") so lookups are cheap. */
function toTree(nodes: SgNode[], prefix = ''): TreeNode[] {
  return nodes.map((node, i) => {
    const key = prefix ? `${prefix}-${i}` : String(i)
    return {
      key,
      label: label(node),
      data: node,
      children: toTree(node.children, key),
      leaf: node.children.length === 0,
    }
  })
}

const tree = computed(() => toTree(devTools.sgNodes?.nodes ?? []))

const filteredTree = computed(() => {
  const term = filter.value.trim().toLowerCase()
  if (!term) return tree.value
  const keep = (nodes: TreeNode[]): TreeNode[] =>
    nodes.flatMap((n) => {
      const children = keep(n.children ?? [])
      const node = n.data as SgNode
      const hit =
        n.label!.toLowerCase().includes(term) ||
        Object.values(node.attributes).some((v) => v.toLowerCase().includes(term))
      return hit || children.length ? [{ ...n, children }] : []
    })
  return keep(tree.value)
})

// While filtering, show every match expanded.
watch(filter, (term) => {
  if (term.trim()) expandedKeys.value = allKeys(filteredTree.value)
})

const selected = computed<SgNode | null>(() => {
  const key = Object.keys(selectionKeys.value)[0]
  if (!key || !devTools.sgNodes) return null
  let nodes = devTools.sgNodes.nodes
  let node: SgNode | undefined
  for (const index of key.split('-').map(Number)) {
    node = nodes[index]
    if (!node) return null
    nodes = node.children
  }
  return node ?? null
})

const selectedAttributes = computed(() => {
  if (!selected.value) return []
  const entries = Object.entries(selected.value.attributes)
  const rank = (k: string) => {
    const i = KEY_ATTRIBUTES.indexOf(k)
    return i === -1 ? KEY_ATTRIBUTES.length : i
  }
  return entries.sort(([a], [b]) => rank(a) - rank(b) || a.localeCompare(b))
})

function allKeys(nodes: TreeNode[], into: Record<string, boolean> = {}) {
  for (const n of nodes) {
    if (n.children?.length) {
      into[n.key] = true
      allKeys(n.children, into)
    }
  }
  return into
}

/** Deepest node with focused="true": the end of the focus chain. */
function findFocused(nodes: SgNode[], prefix = '', depth = 0): { key: string; depth: number } | null {
  let best: { key: string; depth: number } | null = null
  for (const [i, node] of nodes.entries()) {
    const key = prefix ? `${prefix}-${i}` : String(i)
    if (node.attributes.focused === 'true' && (!best || depth > best.depth)) best = { key, depth }
    const inner = findFocused(node.children, key, depth + 1)
    if (inner && (!best || inner.depth > best.depth)) best = inner
  }
  return best
}

const focusedKey = computed(() => findFocused(devTools.sgNodes?.nodes ?? [])?.key ?? null)

function selectKey(key: string) {
  filter.value = ''
  const parts = key.split('-')
  const expanded = { ...expandedKeys.value }
  for (let i = 1; i < parts.length; i++) expanded[parts.slice(0, i).join('-')] = true
  expandedKeys.value = expanded
  selectionKeys.value = { [key]: true }
  nextTick(() => document.querySelector('.sg-tree [aria-selected="true"]')?.scrollIntoView({ block: 'nearest' }))
}

async function load() {
  if (!canLoad.value) return
  await devTools.loadSgNodes(props.rokuId, { scope: scope.value, nodeId: nodeId.value.trim(), sizes: sizes.value })
  selectionKeys.value = {}
  expandedKeys.value = {}
  // Open the top level so there is something to look at.
  for (const n of tree.value) if (n.children?.length) expandedKeys.value[n.key] = true
}

function download() {
  if (!devTools.sgNodes) return
  const blob = new Blob([JSON.stringify(devTools.sgNodes, null, 2)], { type: 'application/json' })
  downloadBlob(blob, snapshotFileName('sgnodes', new Date(devTools.sgNodes.retrievedUtc), 'json'))
}
</script>

<template>
  <div class="space-y-3">
    <form class="flex flex-wrap items-center gap-2" @submit.prevent="load">
      <SelectButton
        v-model="scope"
        :options="scopeOptions"
        option-label="label"
        option-value="value"
        :allow-empty="false"
        size="small"
        aria-label="Which nodes"
      />
      <InputText
        v-if="scope === 'Nodes'"
        v-model="nodeId"
        size="small"
        placeholder="Node id"
        aria-label="Node id"
        class="w-40"
      />
      <label class="flex items-center gap-2 text-xs text-surface-400">
        <Checkbox v-model="sizes" binary input-id="sg-sizes" />
        <span>Memory sizes</span>
      </label>
      <Button
        type="submit"
        :label="devTools.sgNodes ? 'Refresh' : 'Capture'"
        icon="pi pi-sitemap"
        size="small"
        :loading="devTools.sgLoading"
        :disabled="!canLoad"
      />
    </form>

    <Message v-if="devTools.sgError" severity="warn" size="small">
      <strong>{{ devTools.sgError.title }}.</strong> {{ devTools.sgError.userMessage }}
    </Message>

    <p v-if="!devTools.sgNodes && !devTools.sgError" class="text-sm text-surface-500">
      Capture the SceneGraph node tree of the channel in the foreground. Works with developer mode on and a sideloaded
      channel running.
    </p>

    <template v-if="devTools.sgNodes">
      <div class="flex flex-wrap items-center gap-2 text-xs text-surface-500">
        <span>
          {{ devTools.sgNodes.totalNodes.toLocaleString() }} nodes · captured
          {{ new Date(devTools.sgNodes.retrievedUtc).toLocaleTimeString() }}
        </span>
        <span class="flex-1" />
        <Button
          v-if="focusedKey"
          label="Focused node"
          icon="pi pi-bullseye"
          text
          size="small"
          @click="selectKey(focusedKey)"
        />
        <Button label="Expand all" text size="small" @click="expandedKeys = allKeys(filteredTree)" />
        <Button label="Collapse" text size="small" @click="expandedKeys = {}" />
        <Button
          icon="pi pi-download"
          text
          size="small"
          aria-label="Download as JSON"
          title="Download as JSON"
          @click="download"
        />
      </div>

      <div class="grid gap-3 md:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
        <div class="overflow-hidden rounded-lg border border-surface-800">
          <IconField class="border-b border-surface-800">
            <InputIcon class="pi pi-search" />
            <InputText
              v-model="filter"
              size="small"
              placeholder="Filter by type, id or any field"
              aria-label="Filter nodes"
              class="w-full rounded-none border-0"
            />
          </IconField>
          <Tree
            v-model:expanded-keys="expandedKeys"
            v-model:selection-keys="selectionKeys"
            :value="filteredTree"
            selection-mode="single"
            :meta-key-selection="false"
            class="sg-tree max-h-[28rem] overflow-auto !bg-transparent !p-1 text-sm"
          >
            <template #default="{ node }">
              <span class="flex min-w-0 items-center gap-1.5">
                <span class="truncate font-mono text-xs">{{ node.label }}</span>
                <i
                  v-if="(node.data as SgNode).attributes.focused === 'true'"
                  class="pi pi-bullseye text-[10px] text-primary"
                  title="Focused"
                  aria-label="Focused"
                />
                <i
                  v-if="(node.data as SgNode).attributes.visible === 'false'"
                  class="pi pi-eye-slash text-[10px] text-surface-500"
                  title="Not visible"
                  aria-label="Not visible"
                />
              </span>
            </template>
          </Tree>
          <p v-if="!filteredTree.length" class="p-4 text-center text-sm text-surface-500">No matching nodes.</p>
        </div>

        <div class="rounded-lg border border-surface-800 p-3">
          <template v-if="selected">
            <h3 class="mb-2 break-all font-mono text-sm font-semibold">{{ label(selected) }}</h3>
            <p class="mb-3 text-xs text-surface-500">
              {{ selected.children.length }} {{ selected.children.length === 1 ? 'child' : 'children' }}
            </p>
            <dl class="grid grid-cols-[auto_minmax(0,1fr)] gap-x-3 gap-y-1 text-xs">
              <template v-for="[key, value] in selectedAttributes" :key="key">
                <dt class="font-mono text-surface-500">{{ key }}</dt>
                <dd class="break-all font-mono text-surface-200">{{ value === '' ? '""' : value }}</dd>
              </template>
            </dl>
            <p v-if="!selectedAttributes.length" class="text-xs text-surface-500">No fields reported.</p>
          </template>
          <p v-else class="text-sm text-surface-500">Select a node to see its fields.</p>
        </div>
      </div>
    </template>
  </div>
</template>
