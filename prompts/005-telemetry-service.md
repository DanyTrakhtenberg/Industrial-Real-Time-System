Implement TelemetryService.

Requirements:

- Generate telemetry for exactly 20 sensors once per second.
- For each sensor:
  1. Write latest telemetry JSON to Redis key sensor:{id}:latest
  2. Publish telemetry.updated event to RabbitMQ
- If Redis write fails, do not publish RabbitMQ event.
- Add structured logs.
- Add unit tests.
