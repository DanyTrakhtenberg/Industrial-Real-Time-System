export interface SensorDto {
  id: number
  displayName: string
  unit: string
  enabled: boolean
  latestValue: number | null
  latestUnit: string | null
  latestCapturedAt: string | null
}

export interface TelemetryReadingPayload {
  sensorId: number
  value: number
  unit: string
  capturedAt: string
}

export interface TelemetryUpdatedEnvelope {
  schemaVersion: number
  routingKey?: string
  correlationId?: string
  emittedAt: string
  readings: TelemetryReadingPayload[]
}

export interface HistoryItemDto {
  id: number
  sensorId: number
  value: number
  unit: string
  capturedAt: string
}

export interface HistoryResponseDto {
  items: HistoryItemDto[]
  nextPageToken: string
}
