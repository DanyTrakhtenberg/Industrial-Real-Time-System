namespace TelemetryService.Telemetry;

/// <summary>Deterministic pseudo-sensor values (no random walk) for repeatable behavior.</summary>
public sealed class SinusoidalSensorValueGenerator : ISensorValueGenerator
{
    public SensorLatestReading CreateReading(int sensorId, DateTimeOffset capturedAt)
    {
        var t = capturedAt.ToUnixTimeSeconds();
        var value = Math.Sin(t * 0.05 + sensorId * 0.31) * 100.0;
        return new SensorLatestReading(sensorId, Math.Round(value, 4), string.Empty, capturedAt);
    }
}
