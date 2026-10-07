// https://nuxt.com/docs/api/configuration/nuxt-config
import tailwindcss from '@tailwindcss/vite'
import Aura from '@primeuix/themes/aura'

/** Where the .NET API listens during development (see src/SixBench.Api/Properties/launchSettings.json). */
const apiDevTarget = process.env.NUXT_API_DEV_TARGET ?? 'http://localhost:5216'

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  // Pure SPA: the app is generated to static files and served by the API in production.
  ssr: false,
  devtools: { enabled: true },
  modules: ['@pinia/nuxt', '@primevue/nuxt-module', '@nuxt/eslint'],
  css: ['~/assets/css/main.css', 'primeicons/primeicons.css'],
  app: {
    head: {
      title: 'SixBench',
      htmlAttrs: { lang: 'en', class: 'app-dark' },
      meta: [
        { name: 'viewport', content: 'width=device-width, initial-scale=1, viewport-fit=cover' },
        { name: 'description', content: 'Watch and control a Roku through an HDMI capture encoder.' },
        { name: 'theme-color', content: '#09090b' },
      ],
    },
  },
  runtimeConfig: {
    public: {
      apiBase: '/api/v1',
      /** Origin for the stream WebSocket; empty means same origin as the page. */
      streamOrigin: '',
    },
  },
  $development: {
    runtimeConfig: {
      public: {
        // The dev proxy does not forward WebSocket upgrades, so connect straight to the API.
        streamOrigin: apiDevTarget.replace(/^http/, 'ws'),
      },
    },
  },
  primevue: {
    options: {
      ripple: true,
      theme: {
        preset: Aura,
        options: {
          darkModeSelector: '.app-dark',
          cssLayer: { name: 'primevue', order: 'theme, base, primevue' },
        },
      },
    },
  },
  vite: {
    plugins: [tailwindcss()],
  },
  nitro: {
    devProxy: {
      '/api': { target: `${apiDevTarget}/api`, changeOrigin: true, ws: true },
    },
  },
  typescript: {
    strict: true,
  },
  eslint: {
    config: { stylistic: false },
  },
})
