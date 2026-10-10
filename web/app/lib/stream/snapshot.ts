/**
 * Copies the frame currently shown by a renderer (WebCodecs canvas or MSE video) into a PNG at the
 * stream's native resolution.
 */
export async function captureFrame(source: HTMLCanvasElement | HTMLVideoElement): Promise<Blob> {
  const width = source instanceof HTMLVideoElement ? source.videoWidth : source.width
  const height = source instanceof HTMLVideoElement ? source.videoHeight : source.height
  if (!width || !height) throw new Error('There is no video frame to capture yet.')

  const out = document.createElement('canvas')
  out.width = width
  out.height = height
  const ctx = out.getContext('2d')
  if (!ctx) throw new Error('Canvas 2D context is unavailable.')
  ctx.drawImage(source, 0, 0, width, height)

  return new Promise((resolve, reject) =>
    out.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('Could not encode the screenshot.'))), 'image/png'),
  )
}

/**
 * Copies the frame currently shown by a renderer into a small JPEG, for the encoder's thumbnail.
 * @param maxWidth Frames wider than this are scaled down, keeping their aspect ratio.
 */
export async function captureThumbnail(source: HTMLCanvasElement | HTMLVideoElement, maxWidth = 640): Promise<Blob> {
  const sourceWidth = source instanceof HTMLVideoElement ? source.videoWidth : source.width
  const sourceHeight = source instanceof HTMLVideoElement ? source.videoHeight : source.height
  if (!sourceWidth || !sourceHeight) throw new Error('There is no video frame to capture yet.')

  const scale = Math.min(1, maxWidth / sourceWidth)
  const out = document.createElement('canvas')
  out.width = Math.round(sourceWidth * scale)
  out.height = Math.round(sourceHeight * scale)
  const ctx = out.getContext('2d')
  if (!ctx) throw new Error('Canvas 2D context is unavailable.')
  ctx.drawImage(source, 0, 0, out.width, out.height)

  return new Promise((resolve, reject) =>
    out.toBlob(
      (blob) => (blob ? resolve(blob) : reject(new Error('Could not encode the thumbnail.'))),
      'image/jpeg',
      0.8,
    ),
  )
}

/** File name like `living-room-roku-20261008-142233.png` (local time). */
export function snapshotFileName(name: string, at: Date = new Date(), extension = 'png'): string {
  const slug =
    name
      .toLowerCase()
      .normalize('NFKD')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'sixbench'
  const pad = (n: number) => String(n).padStart(2, '0')
  const stamp =
    `${at.getFullYear()}${pad(at.getMonth() + 1)}${pad(at.getDate())}` +
    `-${pad(at.getHours())}${pad(at.getMinutes())}${pad(at.getSeconds())}`
  return `${slug}-${stamp}.${extension}`
}

/** Saves a blob through the browser's download flow. */
export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.click()
  // Give the browser a moment to start the download before revoking.
  setTimeout(() => URL.revokeObjectURL(url), 10_000)
}
