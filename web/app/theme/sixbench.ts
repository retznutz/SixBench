import { definePreset } from '@primeuix/themes'
import Aura from '@primeuix/themes/aura'

/**
 * SixBench design system, derived from the logo (public/sixbench.svg).
 *
 * Two brand colors come from the logo: magenta `#c50cc8` (primary-600) and ink `#151217`.
 * Both scales are stepped in OKLCH at the logo's hue (328°), so every step reads as the same color family.
 * The surface scale is a near-neutral with a trace of that hue, so the greys sit with the magenta
 * instead of fighting it.
 *
 * These tokens feed PrimeVue components and, via tailwindcss-primeui, Tailwind utilities
 * (`bg-surface-900`, `text-primary-400`, `bg-primary`, ...). Use them instead of Tailwind's built-in palettes.
 *
 * Dark-surface contrast (WCAG): surface-0 19.8:1, surface-400 7.5:1, surface-500 4.6:1 on surface-950;
 * white on primary-600 4.9:1; primary-400 7.5:1 on surface-950. So in the dark scheme `primary` /
 * `text-primary` resolve to 400 (accents and magenta text), while filled buttons and the wordmark use the
 * exact brand 600.
 */
export const brand = {
  magenta: '#c50cc8',
  ink: '#151217',
} as const

const primary = {
  50: '#fff2fe',
  100: '#fee3fd',
  200: '#ffc4fd',
  300: '#fc98fa',
  400: '#f067f0',
  500: '#e236e4',
  600: brand.magenta,
  700: '#9f13a1',
  800: '#7c127e',
  900: '#5e135f',
  950: '#3b0b3c',
}

const surface = {
  0: '#ffffff',
  50: '#fbfafb',
  100: '#f5f3f5',
  200: '#e6e4e6',
  300: '#d7d3d6',
  400: '#a49ea3',
  500: '#7d777c',
  600: '#575156',
  700: '#433e43',
  800: '#2a262a',
  900: '#1a171a',
  950: '#0c090b',
}

export const SixBenchPreset = definePreset(Aura, {
  primitive: {
    // Softer corners echo the logo's rounded strokes.
    borderRadius: { none: '0', xs: '4px', sm: '6px', md: '8px', lg: '10px', xl: '14px' },
  },
  semantic: {
    primary,
    focusRing: { width: '2px', style: 'solid', color: '{primary.400}', offset: '2px', shadow: 'none' },
    colorScheme: {
      light: {
        surface,
        primary: {
          color: '{primary.600}',
          contrastColor: '#ffffff',
          hoverColor: '{primary.700}',
          activeColor: '{primary.800}',
        },
        highlight: {
          background: '{primary.50}',
          focusBackground: '{primary.100}',
          color: '{primary.700}',
          focusColor: '{primary.800}',
        },
      },
      dark: {
        surface,
        // PrimeVue uses primary.color for text and accents too (active tabs, steps, checkboxes, text buttons),
        // so on dark surfaces it is the brighter 400 step (7.1:1). Filled buttons are pinned to the exact
        // brand magenta in `components.button` below.
        primary: {
          color: '{primary.400}',
          contrastColor: '{surface.950}',
          hoverColor: '{primary.300}',
          activeColor: '{primary.200}',
        },
        highlight: {
          background: 'color-mix(in srgb, {primary.400}, transparent 84%)',
          focusBackground: 'color-mix(in srgb, {primary.400}, transparent 76%)',
          color: '{primary.100}',
          focusColor: '{primary.50}',
        },
        formField: {
          background: '{surface.950}',
          filledBackground: '{surface.900}',
          borderColor: '{surface.700}',
          hoverBorderColor: '{surface.500}',
          focusBorderColor: '{primary.400}',
        },
      },
    },
  },
  components: {
    button: {
      colorScheme: {
        dark: {
          root: {
            // Filled primary buttons: exact brand magenta, white label (4.9:1); hover darkens to keep contrast.
            primary: {
              background: '{primary.600}',
              hoverBackground: '{primary.700}',
              activeBackground: '{primary.800}',
              borderColor: '{primary.600}',
              hoverBorderColor: '{primary.700}',
              activeBorderColor: '{primary.800}',
              color: '#ffffff',
              hoverColor: '#ffffff',
              activeColor: '#ffffff',
            },
          },
        },
      },
    },
  },
})

/** Theme config consumed by the PrimeVue Nuxt module (`primevue.importTheme` in nuxt.config.ts). */
export default {
  preset: SixBenchPreset,
  options: {
    darkModeSelector: '.app-dark',
    cssLayer: { name: 'primevue', order: 'theme, base, primevue' },
  },
}
