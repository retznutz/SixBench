// https://nuxt.com/docs/api/configuration/nuxt-config
import tailwindcss from '@tailwindcss/vite'

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
        // surface-950 from app/theme/sixbench.ts
        { name: 'theme-color', content: '#0c090b' },
      ],
      link: [
        // Generated from public/sixbench.svg by `npm run brand:assets`. The SVG follows the browser's light/dark theme.
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
        { rel: 'icon', href: '/favicon.ico', sizes: '16x16 32x32 48x48' },
        { rel: 'icon', type: 'image/png', href: '/favicon-32.png', sizes: '32x32' },
        { rel: 'apple-touch-icon', href: '/apple-touch-icon.png' },
        { rel: 'manifest', href: '/site.webmanifest' },
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
    // Imported as a module (not inlined here) so the preset survives intact; options are serialized to JSON.
    importTheme: { from: '~/theme/sixbench.ts', as: 'SixBenchTheme' },
    options: {
      ripple: true,
    },
  },
  vite: {
    plugins: [tailwindcss()],
  },
  nitro: {
    devProxy: {
      '/api': { target: `${apiDevTarget}/api`, changeOrigin: true, ws: true },
      // SignalR (certificate progress). Without WebSocket forwarding it falls back to server-sent events.
      '/hubs': { target: `${apiDevTarget}/hubs`, changeOrigin: true, ws: true },
    },
  },
  typescript: {
    strict: true,
  },
  eslint: {
    config: { stylistic: false },
  },
})
