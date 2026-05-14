import { useCallback, useEffect, useMemo, useState } from 'react'
import { fetchSensors } from '../api/sensors'
import { useTelemetryHub } from '../realtime/useTelemetryHub'
import type { SensorDto, TelemetryUpdatedEnvelope } from '../types/api'

function buildInitialSlots(sensors: SensorDto[]): Map<number, SensorDto> {
  const m = new Map<number, SensorDto>()
  for (let id = 1; id <= 20; id += 1) {
    const row = sensors.find((s) => s.id === id)
    m.set(
      id,
      row ?? {
        id,
        displayName: `Sensor ${id}`,
        unit: '',
        enabled: true,
        latestValue: null,
        latestUnit: null,
        latestCapturedAt: null,
      },
    )
  }
  return m
}

function mergeReading(prev: SensorDto, reading: { sensorId: number; value: number; unit: string; capturedAt: string }): SensorDto {
  return {
    ...prev,
    latestValue: reading.value,
    latestUnit: reading.unit || null,
    latestCapturedAt: reading.capturedAt,
  }
}

export function DashboardPage() {
  const [slots, setSlots] = useState<Map<number, SensorDto> | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    const ac = new AbortController()
    void (async () => {
      try {
        const list = await fetchSensors(ac.signal)
        setSlots(buildInitialSlots(list))
        setLoadError(null)
      } catch (e) {
        if (ac.signal.aborted) return
        setLoadError(e instanceof Error ? e.message : String(e))
        setSlots(buildInitialSlots([]))
      }
    })()
    return () => ac.abort()
  }, [])

  const onTelemetry = useCallback((envelope: TelemetryUpdatedEnvelope) => {
    if (!envelope?.readings?.length) return
    setSlots((prev) => {
      const next = new Map(prev ?? buildInitialSlots([]))
      for (const r of envelope.readings) {
        if (r.sensorId < 1 || r.sensorId > 20) continue
        const current = next.get(r.sensorId)
        if (current) next.set(r.sensorId, mergeReading(current, r))
      }
      return next
    })
  }, [])

  const { status, error: hubError } = useTelemetryHub(onTelemetry)

  const cards = useMemo(() => {
    if (!slots) return []
    return Array.from({ length: 20 }, (_, i) => slots.get(i + 1)!)
  }, [slots])

  return (
    <div className="page dashboard">
      <header className="page-header">
        <h1>Dashboard</h1>
        <p className="muted">
          Live view of 20 sensors. REST seeds metadata; SignalR pushes <code>telemetryUpdated</code>.
        </p>
        <div className="status-bar">
          <span className={`pill pill-${status}`}>{status}</span>
          {hubError ? <span className="error-inline">{hubError}</span> : null}
          {loadError ? <span className="error-inline">REST: {loadError}</span> : null}
        </div>
      </header>

      {!slots ? (
        <p className="muted">Loading sensors…</p>
      ) : (
        <div className="sensor-grid">
          {cards.map((s) => (
            <article key={s.id} className="sensor-card">
              <div className="sensor-card__head">
                <span className="sensor-id">#{s.id}</span>
                <span className={`dot ${s.enabled ? 'on' : 'off'}`} title={s.enabled ? 'enabled' : 'disabled'} />
              </div>
              <h2 className="sensor-name">{s.displayName}</h2>
              <dl className="sensor-metrics">
                <div>
                  <dt>Value</dt>
                  <dd>{s.latestValue != null ? s.latestValue.toFixed(4) : '—'}</dd>
                </div>
                <div>
                  <dt>Unit</dt>
                  <dd>{(s.latestUnit ?? s.unit) || '—'}</dd>
                </div>
                <div>
                  <dt>Captured</dt>
                  <dd className="mono">{s.latestCapturedAt ? new Date(s.latestCapturedAt).toLocaleString() : '—'}</dd>
                </div>
              </dl>
            </article>
          ))}
        </div>
      )}
    </div>
  )
}
