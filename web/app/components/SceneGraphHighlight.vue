<script setup lang="ts">
import { containRect, DEFAULT_UI_SIZE, isHidden, parseBounds } from '~/lib/scenegraph/bounds'

/**
 * Outlines the SceneGraph node selected (or hovered) in the inspector on top of the video.
 * Goes in VideoSurface's slot: it fills the surface, finds where the letterboxed picture is drawn,
 * and places the node's bounds in it as percentages of the channel's UI size, so page scroll,
 * layout scaling, fullscreen and the capture resolution don't matter.
 */
const devTools = useRokuDevToolsStore()
const stream = useStreamStore()

const layer = ref<HTMLElement | null>(null)
const layerSize = reactive({ width: 0, height: 0 })
let observer: ResizeObserver | undefined

onMounted(() => {
  observer = new ResizeObserver(([entry]) => {
    if (!entry) return
    layerSize.width = entry.contentRect.width
    layerSize.height = entry.contentRect.height
  })
  if (layer.value) observer.observe(layer.value)
})
onBeforeUnmount(() => observer?.disconnect())

const path = computed(() => devTools.sgHighlightPath)
const node = computed(() => path.value?.at(-1) ?? null)
const bounds = computed(() => parseBounds(node.value?.attributes.bounds))
const ui = computed(() => devTools.sgUiSize ?? DEFAULT_UI_SIZE)
const hidden = computed(() => (path.value ? isHidden(path.value) : false))

/** Where the picture is drawn: the canvas/video uses object-contain, so it is letterboxed by the frame's aspect. */
const frameStyle = computed(() => {
  const r = containRect(
    layerSize.width,
    layerSize.height,
    stream.stats?.width || ui.value.width,
    stream.stats?.height || ui.value.height,
  )
  return { left: `${r.x}px`, top: `${r.y}px`, width: `${r.width}px`, height: `${r.height}px` }
})

const pct = (value: number, of: number) => `${(value / of) * 100}%`

const boxStyle = computed(() => {
  const b = bounds.value
  if (!b) return null
  return {
    left: pct(b.x, ui.value.width),
    top: pct(b.y, ui.value.height),
    width: pct(b.width, ui.value.width),
    height: pct(b.height, ui.value.height),
  }
})

const offScreen = computed(() => {
  const b = bounds.value
  return !!b && (b.x >= ui.value.width || b.y >= ui.value.height || b.x + b.width <= 0 || b.y + b.height <= 0)
})

const label = computed(() => {
  const n = node.value
  if (!n) return ''
  const name = n.attributes.name ? `${n.type} #${n.attributes.name}` : n.type
  const b = bounds.value
  return b ? `${name} · ${Math.round(b.width)}×${Math.round(b.height)}` : name
})

/** Keep the label inside the picture: below the box near the top edge, right-aligned past the middle. */
const labelBelow = computed(() => (bounds.value?.y ?? 0) < ui.value.height * 0.06)
const labelRight = computed(() => (bounds.value?.x ?? 0) > ui.value.width / 2)
</script>

<template>
  <div ref="layer" class="pointer-events-none absolute inset-0" aria-hidden="true">
    <div v-if="node && bounds" class="absolute overflow-hidden" :style="frameStyle">
      <p
        v-if="offScreen"
        class="absolute left-2 top-2 rounded bg-primary-600 px-1.5 py-0.5 font-mono text-[11px] text-white"
      >
        {{ label }} is off screen
      </p>
      <div
        v-else
        class="absolute min-h-0.5 min-w-0.5 rounded-sm border-2 bg-primary-400/15"
        :class="hidden ? 'border-dashed border-primary-300 opacity-70' : 'border-primary-500'"
        :style="boxStyle ?? undefined"
      >
        <span
          class="absolute whitespace-nowrap rounded-sm bg-primary-600 px-1.5 py-0.5 font-mono text-[11px] leading-none text-white"
          :class="[labelBelow ? 'top-full mt-0.5' : 'bottom-full mb-0.5', labelRight ? '-right-0.5' : '-left-0.5']"
        >
          {{ label }}{{ hidden ? ' (hidden)' : '' }}
        </span>
      </div>
    </div>
  </div>
</template>
