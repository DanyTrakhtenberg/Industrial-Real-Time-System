import { useEffect, useMemo, useState } from 'react'
import { fetchSensorHistory, fetchSensors } from '../api/sensors'
import type { HistoryItemDto, SensorDto } from '../types/api'

export function HistoryPage() {
  const [sensors, setSensors] = useState<SensorDto[]>([])
  const [sensorId, setSensorId] = useState(1)
  const [items, setItems] = useState<HistoryItemDto[]>([])
  const [nextToken, setNextToken] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const ac = new AbortController()
    void (async () => {
      try {
        const list = await fetchSensors(ac.signal)
        if (!ac.signal.aborted) setSensors(list)
      } catch {
        /* dropdown still works with numeric ids */
      }
    })()
    return () => ac.abort()
  }, [])

  const sensorOptions = useMemo(() => {
    if (sensors.length) return sensors
    return Array.from({ length: 20 }, (_, i) => ({
      id: i + 1,
      displayName: `Sensor ${i + 1}`,
      unit: '',
      enabled: true,
      latestValue: null,
      latestUnit: null,
      latestCapturedAt: null,
    }))
  }, [sensors])

  useEffect(() => {
    const ac = new AbortController()
    void (async () => {
      setLoading(true)
      setError(null)
      setItems([])
      setNextToken(null)
      try {
        const res = await fetchSensorHistory(sensorId, { pageSize: 50 }, ac.signal)
        if (ac.signal.aborted) return
        setItems(res.items)
        setNextToken(res.nextPageToken || null)
      } catch (e) {
        if (ac.signal.aborted) return
        setError(e instanceof Error ? e.message : String(e))
      } finally {
        if (!ac.signal.aborted) setLoading(false)
      }
    })()
    return () => ac.abort()
  }, [sensorId])

  const onLoadMore = () => {
    if (!nextToken) return
    const token = nextToken
    void (async () => {
      setLoading(true)
      setError(null)
      try {
        const res = await fetchSensorHistory(sensorId, { pageSize: 50, pageToken: token })
        setItems((prev) => [...prev, ...res.items])
        setNextToken(res.nextPageToken || null)
      } catch (e) {
        setError(e instanceof Error ? e.message : String(e))
      } finally {
        setLoading(false)
      }
    })()
  }

  return (
    <div className="page history">
      <header className="page-header">
        <h1>History</h1>
        <p className="muted">
          Per-sensor history from <code>GET /api/sensors/{'{id}'}/history</code>.
        </p>
      </header>

      <div className="history-controls">
        <label htmlFor="sensor-select" className="label">
          Sensor
        </label>
        <select
          id="sensor-select"
          className="select"
          value={sensorId}
          onChange={(e) => setSensorId(Number(e.target.value))}
        >
          {sensorOptions.map((s) => (
            <option key={s.id} value={s.id}>
              #{s.id} — {s.displayName}
            </option>
          ))}
        </select>
      </div>

      {loading && items.length === 0 ? <p className="muted">Loading history…</p> : null}
      {error ? <p className="error-block">{error}</p> : null}

      {items.length > 0 ? (
        <>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Row id</th>
                  <th>Value</th>
                  <th>Unit</th>
                  <th>Captured (UTC)</th>
                </tr>
              </thead>
              <tbody>
                {items.map((r) => (
                  <tr key={`${r.id}-${r.capturedAt}`}>
                    <td className="mono">{r.id}</td>
                    <td className="mono">{r.value.toFixed(4)}</td>
                    <td>{r.unit || '—'}</td>
                    <td className="mono">{new Date(r.capturedAt).toISOString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {nextToken ? (
            <button type="button" className="btn secondary" disabled={loading} onClick={onLoadMore}>
              {loading ? 'Loading…' : 'Load more'}
            </button>
          ) : null}
        </>
      ) : !loading && !error ? (
        <p className="muted">No history rows for this sensor yet.</p>
      ) : null}
    </div>
  )
}
