import { useEffect, useState } from 'react'
import { fetchSensors } from '../api/sensors'
import type { SensorDto } from '../types/api'

export function SensorsPage() {
  const [rows, setRows] = useState<SensorDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const ac = new AbortController()
    void (async () => {
      try {
        setLoading(true)
        const data = await fetchSensors(ac.signal)
        setRows(data)
        setError(null)
      } catch (e) {
        if (ac.signal.aborted) return
        setError(e instanceof Error ? e.message : String(e))
      } finally {
        if (!ac.signal.aborted) setLoading(false)
      }
    })()
    return () => ac.abort()
  }, [])

  return (
    <div className="page sensors">
      <header className="page-header">
        <h1>Sensors</h1>
        <p className="muted">Data from <code>GET /api/sensors</code> (SQL metadata + Redis latest merged by API).</p>
      </header>

      {loading ? <p className="muted">Loading…</p> : null}
      {error ? <p className="error-block">{error}</p> : null}

      {!loading && !error ? (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Id</th>
                <th>Name</th>
                <th>Unit</th>
                <th>Enabled</th>
                <th>Latest value</th>
                <th>Latest captured</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id}>
                  <td className="mono">{r.id}</td>
                  <td>{r.displayName}</td>
                  <td>{r.unit || '—'}</td>
                  <td>{r.enabled ? 'yes' : 'no'}</td>
                  <td className="mono">{r.latestValue != null ? r.latestValue.toFixed(4) : '—'}</td>
                  <td className="mono">
                    {r.latestCapturedAt ? new Date(r.latestCapturedAt).toLocaleString() : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  )
}
