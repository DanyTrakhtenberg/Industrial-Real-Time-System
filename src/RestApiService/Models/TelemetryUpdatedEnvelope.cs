namespace RestApiService.Models;

public sealed class TelemetryUpdatedEnvelope
{
    public int SchemaVersion { get; set; }

    public string? RoutingKey { get; set; }

    public string? CorrelationId { get; set; }

    public DateTimeOffset EmittedAt { get; set; }

    public List<TelemetryReadingPayload>? Readings { get; set; }
}

public sealed class TelemetryReadingPayload
{
    public int SensorId { get; set; }

    public double Value { get; set; }

    public string? Unit { get; set; }

    public DateTimeOffset CapturedAt { get; set; }
}
