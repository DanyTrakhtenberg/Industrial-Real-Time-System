/**
 * API base URL. If `VITE_API_BASE_URL` is unset or empty, use same-origin relative paths
 * (`/api/...`, `/hubs/...`) so Docker nginx / Vite proxy can forward to the REST service
 * without cross-origin issues (embedded browsers, strict CORS).
 */
export function getApiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL as string | undefined
  if (raw != null && String(raw).trim() !== '') {
    return String(raw).trim().replace(/\/$/, '')
  }
  return ''
}

export function getSignalRHubUrl(): string {
  return `${getApiBaseUrl()}/hubs/sensor-stream`
}
