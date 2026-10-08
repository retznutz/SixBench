/**
 * Generates the brand assets in public/ from the source logo, public/sixbench.svg.
 * Run after changing the logo:  npm run brand:assets
 *
 * Expects the Illustrator export layout: two top-level <g> groups, the remote first and the wordmark second,
 * with the ink (dark) colors and the magenta set in its <style> block or as default (black) fills.
 *
 * Outputs (all in public/):
 *   brand/sixbench-dark.svg   Full logo for dark surfaces: ink recolored light, cropped to the artwork.
 *   favicon.svg               Just the remote; its outline follows the browser's light/dark theme.
 *   favicon.ico, favicon-32.png, apple-touch-icon.png, icon-192.png, icon-512.png, icon-maskable-512.png
 *                             The remote on a dark tile, for browsers and home screens that need bitmaps.
 */
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { Resvg } from '@resvg/resvg-js'

const PUBLIC = join(dirname(fileURLToPath(import.meta.url)), '..', 'public')
/** surface-100 in app/theme/sixbench.ts: replaces the logo's ink on dark backgrounds. */
const LIGHT_INK = '#f5f3f5'
/** surface-900: background tile for bitmap icons. */
const TILE = '#1a171a'

const source = readFileSync(join(PUBLIC, 'sixbench.svg'), 'utf8')

const style = source.match(/<style>([\s\S]*?)<\/style>/)?.[1] ?? ''
const groups = [...source.matchAll(/<g>([\s\S]*?)<\/g>/g)].map((m) => m[1])
if (groups.length !== 2) {
  throw new Error(`Expected 2 top-level <g> groups in sixbench.svg (remote, wordmark); found ${groups.length}.`)
}
const [remote, wordmark] = groups

/** Relative luminance of a #rrggbb color. */
function luminance(hex) {
  const [r, g, b] = [1, 3, 5].map((i) => {
    const c = parseInt(hex.slice(i, i + 2), 16) / 255
    return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
  })
  return 0.2126 * r + 0.7152 * g + 0.0722 * b
}

/** Replaces dark (ink) colors in CSS with `replacement`; the magenta and other light colors are kept. */
const swapInk = (css, replacement) =>
  css.replace(/#[0-9a-fA-F]{6}\b/g, (hex) => (luminance(hex) < 0.05 ? replacement : hex))

/** Inner bounding box (including strokes) of an SVG document. */
function bbox(svg) {
  const box = new Resvg(svg).innerBBox()
  if (!box) throw new Error('Could not measure the logo artwork.')
  return { x: box.x, y: box.y, width: box.width, height: box.height }
}

const svgDoc = (viewBox, body, attrs = '') =>
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${viewBox}"${attrs}>${body}</svg>\n`

const round = (n) => Math.round(n * 100) / 100

// Full logo for dark surfaces. Unstyled paths (the wordmark) use the root fill, so set it to the light ink.
const full = bbox(source)
const pad = 4
mkdirSync(join(PUBLIC, 'brand'), { recursive: true })
writeFileSync(
  join(PUBLIC, 'brand', 'sixbench-dark.svg'),
  svgDoc(
    `${full.x - pad} ${full.y - pad} ${full.width + 2 * pad} ${full.height + 2 * pad}`,
    `<title>SixBench</title><style>${swapInk(style, LIGHT_INK)}</style><g>${remote}</g><g>${wordmark}</g>`,
    ` fill="${LIGHT_INK}"`,
  ),
)

// The remote alone, centred in a square.
const remoteBox = bbox(svgDoc('0 0 1000 1000', `<style>${style}</style><g>${remote}</g>`))
const side = Math.max(remoteBox.width, remoteBox.height)
const square = (padding) => {
  const s = side * (1 + 2 * padding)
  const x = remoteBox.x + remoteBox.width / 2 - s / 2
  const y = remoteBox.y + remoteBox.height / 2 - s / 2
  return `${round(x)} ${round(y)} ${round(s)} ${round(s)}`
}

// SVG favicon: ink via a custom property so the outline flips with the browser's color scheme.
writeFileSync(
  join(PUBLIC, 'favicon.svg'),
  svgDoc(
    square(0.04),
    `<style>:root{--ink:#151217}@media (prefers-color-scheme:dark){:root{--ink:${LIGHT_INK}}}` +
      `${swapInk(style, 'var(--ink)')}</style><g>${remote}</g>`,
  ),
)

/** The remote (light ink) on a dark tile, rendered to PNG. `padding` is the margin as a fraction of the size. */
function tilePng(size, { radius = 0.22, padding = 0.16, opaque = false } = {}) {
  const inner = 512 * (1 - 2 * padding)
  const offset = 512 * padding
  const tile = opaque
    ? `<rect width="512" height="512" fill="${TILE}"/>`
    : `<rect width="512" height="512" rx="${512 * radius}" fill="${TILE}"/>`
  const svg = svgDoc(
    '0 0 512 512',
    `<style>${swapInk(style, LIGHT_INK)}</style>${tile}` +
      `<svg x="${offset}" y="${offset}" width="${inner}" height="${inner}" viewBox="${square(0)}"><g>${remote}</g></svg>`,
  )
  return new Resvg(svg, { fitTo: { mode: 'width', value: size } }).render().asPng()
}

/** An .ico holding PNG images (supported by every current browser and Windows Vista+). */
function ico(images) {
  const header = Buffer.alloc(6 + 16 * images.length)
  header.writeUInt16LE(0, 0)
  header.writeUInt16LE(1, 2)
  header.writeUInt16LE(images.length, 4)
  let offset = header.length
  images.forEach(({ size, png }, i) => {
    const entry = 6 + 16 * i
    header.writeUInt8(size >= 256 ? 0 : size, entry)
    header.writeUInt8(size >= 256 ? 0 : size, entry + 1)
    header.writeUInt16LE(1, entry + 4)
    header.writeUInt16LE(32, entry + 6)
    header.writeUInt32LE(png.length, entry + 8)
    header.writeUInt32LE(offset, entry + 12)
    offset += png.length
  })
  return Buffer.concat([header, ...images.map((i) => i.png)])
}

// Small sizes get less padding so the remote stays legible.
const small = { padding: 0.1 }
writeFileSync(join(PUBLIC, 'favicon.ico'), ico([16, 32, 48].map((size) => ({ size, png: tilePng(size, small) }))))
writeFileSync(join(PUBLIC, 'favicon-32.png'), tilePng(32, small))
// iOS rounds the corners itself and shows transparency as black, so this one is full-bleed.
writeFileSync(join(PUBLIC, 'apple-touch-icon.png'), tilePng(180, { opaque: true, padding: 0.14 }))
writeFileSync(join(PUBLIC, 'icon-192.png'), tilePng(192))
writeFileSync(join(PUBLIC, 'icon-512.png'), tilePng(512))
// Maskable icons are cropped by the OS; keep the remote inside the central 80% safe zone.
writeFileSync(join(PUBLIC, 'icon-maskable-512.png'), tilePng(512, { opaque: true, padding: 0.2 }))

console.log('Brand assets written to', PUBLIC)
