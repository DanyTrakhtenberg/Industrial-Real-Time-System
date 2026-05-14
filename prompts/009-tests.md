Implement tests.

Unit tests:
- TelemetryService generates exactly 20 sensors.
- TelemetryService writes Redis before RabbitMQ.
- RestApiService merges SQL sensor metadata with Redis latest value.
- SqlDataService saves and retrieves telemetry.

Integration tests:
- Start required services.
- Verify all 20 sensors produce real-time telemetry.
- Verify SignalR receives telemetry from all 20 sensors.