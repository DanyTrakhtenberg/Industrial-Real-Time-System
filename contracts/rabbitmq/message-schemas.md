# RabbitMQ — message and routing skeleton

Conventions are **design-level** until services publish/consume them.

## Exchange

| Name | Type | Purpose |
|------|------|---------|
| `industrial.topic` | topic | Cross-service integration events |

## Suggested routing keys

| Routing key | Producer | Consumer(s) | Notes |
|-------------|----------|-------------|--------|
| `telemetry.updated` | IoT Telemetry Service | REST API | Payload mirrors `TelemetryBatch` JSON shape (see below) |
| `config.sensor.changed` | SQL Data Service (optional) | IoT Telemetry Service, REST API | Emitted after successful commit |

## Queue bindings (illustrative)

- `restapi.telemetry` ← bind `telemetry.*`
- `telemetry.config` ← bind `config.*`

## JSON envelope (illustrative)

Align field names with `src/Shared.Contracts/Protos/telemetry/v1/telemetry.proto` for `TelemetryBatch` / `TelemetryReading`.

```json
{
  "schemaVersion": 1,
  "routingKey": "telemetry.updated",
  "correlationId": "uuid",
  "emittedAt": "2026-05-14T12:00:00Z",
  "readings": [
    {
      "sensorId": 1,
      "value": 0.0,
      "unit": "",
      "capturedAt": "2026-05-14T12:00:00Z"
    }
  ]
}
```
