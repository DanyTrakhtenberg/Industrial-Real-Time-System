Implement RestApiService.

Requirements:
- REST endpoints:
  GET /api/sensors
  GET /api/sensors/{id}
  GET /api/sensors/{id}/history
  GET /api/system/status
- Read sensor metadata/history from SqlDataService using gRPC.
- Read latest sensor values from Redis.
- Merge SQL metadata + Redis latest value in sensor responses.
- Add SignalR hub at /hubs/telemetry.
- Add RabbitMQ background consumer for telemetry.updated.
- Consumer should push telemetry to SignalR and save history through gRPC.