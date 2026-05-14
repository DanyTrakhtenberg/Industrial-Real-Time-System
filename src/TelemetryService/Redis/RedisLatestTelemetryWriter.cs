using System.Text.Json;
using StackExchange.Redis;
using TelemetryService.Telemetry;

namespace TelemetryService.Redis;

public sealed class RedisLatestTelemetryWriter(
    IConnectionMultiplexer redis,
    ILogger<RedisLatestTelemetryWriter> logger) : IRedisLatestTelemetryWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<bool> TryWriteLatestJsonAsync(int sensorId, string json, CancellationToken cancellationToken = default)
    {
        var key = $"sensor:{sensorId}:latest";
        try
        {
            var db = redis.GetDatabase();
            var ok = await db.StringSetAsync(key, json).ConfigureAwait(false);
            if (!ok)
            {
                logger.LogWarning("Redis StringSet returned false for {RedisKey} sensor {SensorId}", key, sensorId);
                return false;
            }

            logger.LogDebug("Redis latest telemetry written for {SensorId} key {RedisKey}", sensorId, key);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis write failed for sensor {SensorId} key {RedisKey}", sensorId, key);
            return false;
        }
    }

    public static string SerializeReading(SensorLatestReading reading) =>
        JsonSerializer.Serialize(reading, JsonOptions);
}
