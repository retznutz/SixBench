import type { RokuDevice } from '~/types/roku-device'

/**
 * URL of the Roku's built-in developer web page (Development Application Installer).
 * It is served on port 80 and only exists once developer mode is enabled on the Roku.
 */
export function rokuDevPageUrl(roku: Pick<RokuDevice, 'ipAddress'>): string {
  const host = roku.ipAddress.includes(':') ? `[${roku.ipAddress}]` : roku.ipAddress
  return `http://${host}/`
}
