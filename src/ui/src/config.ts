export function getApiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL
  const base = raw && raw.length > 0 ? raw : 'http://localhost:5051'
  return base.replace(/\/$/, '')
}

export function getSignalRHubUrl(): string {
  return `${getApiBaseUrl()}/hubs/telemetry`
}
