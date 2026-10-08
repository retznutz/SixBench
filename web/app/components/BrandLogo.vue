<script setup lang="ts">
/**
 * The SixBench logo for dark surfaces: public/brand/sixbench-dark.svg, generated from public/sixbench.svg
 * by `npm run brand:assets`. `sm` fits the header; `lg` is for the sign-in page.
 */
withDefaults(defineProps<{ size?: 'sm' | 'lg' }>(), { size: 'sm' })
</script>

<template>
  <img src="/brand/sixbench-dark.svg" alt="SixBench" :class="size === 'lg' ? 'h-28 w-auto' : 'h-11 w-auto'" />
</template>
