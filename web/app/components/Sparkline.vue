<script setup lang="ts">
/**
 * Single-series trend line for a stat tile. Hovering emits the sample index so the tile can show
 * that sample's value; a crosshair and dot mark it.
 */
const props = withDefaults(
  defineProps<{
    values: (number | null)[]
    /** Total slots on the x-axis, so the line fills in from the left as samples arrive. */
    capacity: number
    /** Anchor the y-axis at zero (magnitudes) instead of the data's own range (small drifts). */
    zeroBased?: boolean
    label: string
  }>(),
  { zeroBased: false },
)
const emit = defineEmits<{ hover: [index: number | null] }>()

const W = 240
const H = 40
const PAD = 3

const hovered = ref<number | null>(null)

const domain = computed(() => {
  const nums = props.values.filter((v): v is number => v != null)
  if (!nums.length) return { min: 0, max: 1 }
  const max = Math.max(...nums)
  const min = props.zeroBased ? 0 : Math.min(...nums)
  return max === min ? { min: min - 1, max: max + 1 } : { min, max }
})

const step = computed(() => W / Math.max(1, props.capacity - 1))
const offset = computed(() => props.capacity - props.values.length)

function x(i: number) {
  return (offset.value + i) * step.value
}

function y(v: number) {
  const { min, max } = domain.value
  return H - PAD - ((v - min) / (max - min)) * (H - 2 * PAD)
}

/** Line segments; null samples break the line rather than drawing to zero. */
const paths = computed(() => {
  const out: string[] = []
  let d = ''
  props.values.forEach((v, i) => {
    if (v == null) {
      if (d) out.push(d)
      d = ''
      return
    }
    d += `${d ? 'L' : 'M'}${x(i).toFixed(1)},${y(v).toFixed(1)}`
  })
  if (d) out.push(d)
  return out
})

const last = computed(() => {
  for (let i = props.values.length - 1; i >= 0; i--) {
    const v = props.values[i]
    if (v != null) return { i, v }
  }
  return null
})

const marker = computed(() => {
  const i = hovered.value ?? last.value?.i
  if (i == null) return null
  const v = props.values[i]
  return v == null ? null : { x: x(i), y: y(v) }
})

function onMove(event: PointerEvent) {
  const rect = (event.currentTarget as SVGElement).getBoundingClientRect()
  const slot = Math.round(((event.clientX - rect.left) / rect.width) * (props.capacity - 1))
  const i = Math.min(props.values.length - 1, Math.max(0, slot - offset.value))
  hovered.value = props.values.length ? i : null
  emit('hover', hovered.value)
}

function onLeave() {
  hovered.value = null
  emit('hover', null)
}
</script>

<template>
  <div class="relative">
    <svg
      :viewBox="`0 0 ${W} ${H}`"
      preserveAspectRatio="none"
      class="h-10 w-full touch-none overflow-visible text-primary"
      role="img"
      :aria-label="label"
      @pointermove="onMove"
      @pointerleave="onLeave"
    >
      <line
        :x1="0"
        :x2="W"
        :y1="H - PAD"
        :y2="H - PAD"
        class="stroke-surface-800"
        stroke-width="1"
        vector-effect="non-scaling-stroke"
      />
      <line
        v-if="hovered != null && marker"
        :x1="marker.x"
        :x2="marker.x"
        y1="0"
        :y2="H"
        class="stroke-surface-600"
        stroke-width="1"
        vector-effect="non-scaling-stroke"
      />
      <path
        v-for="(d, i) in paths"
        :key="i"
        :d="d"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linejoin="round"
        stroke-linecap="round"
        vector-effect="non-scaling-stroke"
      />
    </svg>
    <!-- The dot sits in HTML so it stays round under preserveAspectRatio="none". -->
    <span
      v-if="marker"
      class="pointer-events-none absolute size-2 -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary ring-2 ring-surface-900"
      :style="{ left: `${(marker.x / W) * 100}%`, top: `${(marker.y / H) * 100}%` }"
    />
  </div>
</template>
