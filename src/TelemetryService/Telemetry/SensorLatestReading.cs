namespace TelemetryService.Telemetry;

/// <summary>Latest reading stored as JSON in Redis and included in AMQP payloads.</summary>
public sealed record SensorLatestReading(int SensorId, double Value, string Unit, DateTimeOffset CapturedAt);
