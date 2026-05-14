import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr'
import { startTransition, useCallback, useEffect, useRef, useState } from 'react'
import { getSignalRHubUrl } from '../config'
import type { TelemetryUpdatedEnvelope } from '../types/api'

export type HubConnectionStatus = 'idle' | 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

export function useTelemetryHub(
  onTelemetry: (envelope: TelemetryUpdatedEnvelope) => void,
): { status: HubConnectionStatus; error: string | null } {
  const [status, setStatus] = useState<HubConnectionStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  const handlerRef = useRef(onTelemetry)

  useEffect(() => {
    handlerRef.current = onTelemetry
  }, [onTelemetry])

  const stableHandler = useCallback((envelope: TelemetryUpdatedEnvelope) => {
    handlerRef.current(envelope)
  }, [])

  useEffect(() => {
    const hubUrl = getSignalRHubUrl()
    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: false })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(import.meta.env.DEV ? 'Information' : 'Warning')
      .build()

    startTransition(() => {
      setStatus('connecting')
      setError(null)
    })

    connection.on('telemetryUpdated', (payload: TelemetryUpdatedEnvelope) => {
      stableHandler(payload)
    })

    connection.onreconnecting(() => setStatus('reconnecting'))
    connection.onreconnected(() => setStatus('connected'))
    connection.onclose(() => setStatus('disconnected'))

    let cancelled = false
    void (async () => {
      try {
        await connection.start()
        if (!cancelled) setStatus('connected')
      } catch (e) {
        if (!cancelled) {
          setStatus('disconnected')
          setError(e instanceof Error ? e.message : String(e))
        }
      }
    })()

    return () => {
      cancelled = true
      void connection.stop()
    }
  }, [stableHandler])

  return { status, error }
}
