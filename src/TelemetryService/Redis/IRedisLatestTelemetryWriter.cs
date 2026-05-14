namespace TelemetryService.Redis;

public interface IRedisLatestTelemetryWriter
{
    /// <summary>Writes JSON to <c>sensor:{id}:latest</c>. Returns false if the write did not succeed.</summary>
    Task<bool> TryWriteLatestJsonAsync(int sensorId, string json, CancellationToken cancellationToken = default);
}
