<script setup lang="ts">
import type { DevChannelAction } from '~/stores/rokuDevChannel'

/** Shows the outcome of the last developer action, if it is one of `actions`. */
const props = defineProps<{ actions: DevChannelAction[] }>()

const devChannel = useRokuDevChannelStore()

const mine = computed(() => devChannel.lastAction != null && props.actions.includes(devChannel.lastAction))
const icon = (type: string) =>
  type === 'error'
    ? 'pi pi-times-circle text-red-400'
    : type === 'success'
      ? 'pi pi-check-circle text-green-400'
      : 'pi pi-info-circle text-surface-400'
</script>

<template>
  <template v-if="mine">
    <Message v-if="devChannel.lastError" severity="warn" size="small">
      <strong>{{ devChannel.lastError.title }}.</strong> {{ devChannel.lastError.userMessage }}
    </Message>
    <Message v-else-if="devChannel.lastResult" severity="success" size="small">
      <p>{{ devChannel.lastResult.summary }}</p>
      <ul v-if="devChannel.lastResult.messages.length" class="mt-1 space-y-0.5 text-xs">
        <li v-for="m in devChannel.lastResult.messages" :key="m.type + m.text" class="flex items-start gap-1.5">
          <i :class="icon(m.type)" class="mt-0.5 text-[11px]" aria-hidden="true" />
          <span>{{ m.text }}</span>
        </li>
      </ul>
    </Message>
  </template>
</template>
