using System.Text.Json;
using RestApiService.Models;
using StackExchange.Redis;

namespace RestApiService.Redis;

public interface ISensorLatestRedisReader
{
    Task<SensorLatestDto?> TryGetLatestAsync(int sensorId, CancellationToken cancellationToken = default);
}

public sealed class SensorLatestRedisReader(
    IConnectionMultiplexer redis,
    ILogger<SensorLatestRedisReader> logger) : ISensorLatestRedisReader
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<SensorLatestDto?> TryGetLatestAsync(int sensorId, CancellationToken cancellationToken = default)
    {
        var key = $"sensor:{sensorId}:latest";
        try
        {
            var db = redis.GetDatabase();
            var val = await db.StringGetAsync(key).ConfigureAwait(false);
            if (val.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<SensorLatestDto>(val!, Json);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read or parse Redis key {RedisKey}", key);
            return null;
        }
    }
}
