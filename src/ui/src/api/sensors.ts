import { getApiBaseUrl } from '../config'
import type { HistoryResponseDto, SensorDto } from '../types/api'

/** Omit credentials on cross-origin API calls (avoids CORS preflight failures in embedded / strict browsers). */
function apiFetchInit(signal?: AbortSignal): RequestInit {
  return { signal, credentials: 'omit' }
}

async function parseJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new Error(`${response.status} ${response.statusText}${text ? `: ${text}` : ''}`)
  }
  return (await response.json()) as T
}

export async function fetchSensors(signal?: AbortSignal): Promise<SensorDto[]> {
  const url = `${getApiBaseUrl()}/api/sensors`
  const res = await fetch(url, apiFetchInit(signal))
  return parseJson<SensorDto[]>(res)
}

export async function fetchSensorHistory(
  sensorId: number,
  opts?: { from?: string; to?: string; pageSize?: number; pageToken?: string },
  signal?: AbortSignal,
): Promise<HistoryResponseDto> {
  const params = new URLSearchParams()
  if (opts?.from) params.set('from', opts.from)
  if (opts?.to) params.set('to', opts.to)
  if (opts?.pageSize != null) params.set('pageSize', String(opts.pageSize))
  if (opts?.pageToken) params.set('pageToken', opts.pageToken)
  const qs = params.toString()
  const url = `${getApiBaseUrl()}/api/sensors/${sensorId}/history${qs ? `?${qs}` : ''}`
  const res = await fetch(url, apiFetchInit(signal))
  return parseJson<HistoryResponseDto>(res)
}
