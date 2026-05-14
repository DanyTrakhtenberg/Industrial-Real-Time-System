namespace TelemetryService.Telemetry;

public interface ISensorValueGenerator
{
    SensorLatestReading CreateReading(int sensorId, DateTimeOffset capturedAt);
}
