using System.Text.Json;
using TelemetryService.Messaging;
using TelemetryService.Redis;

namespace TelemetryService.Telemetry;

public sealed class TelemetryCycleExecutor(
    IRedisLatestTelemetryWriter redis,
    IRabbitMqTelemetryPublisher mq,
    ISensorValueGenerator generator,
    ILogger<TelemetryCycleExecutor> logger)
{
    private static readonly JsonSerializerOptions EnvelopeJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>One 1 Hz tick: all 20 sensors. RabbitMQ publish only after successful Redis write for that sensor.</summary>
    public async Task RunOnceAsync(DateTimeOffset tickTime, CancellationToken cancellationToken = default)
    {
        for (var sensorId = 1; sensorId <= 20; sensorId++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var reading = generator.CreateReading(sensorId, tickTime);
            var latestJson = RedisLatestTelemetryWriter.SerializeReading(reading);
            var redisOk = await redis.TryWriteLatestJsonAsync(sensorId, latestJson, cancellationToken).ConfigureAwait(false);
            if (!redisOk)
            {
                logger.LogWarning(
                    "Skipping RabbitMQ publish for sensor {SensorId} because Redis write failed",
                    sensorId);
                continue;
            }

            var envelope = new
            {
                schemaVersion = 1,
                routingKey = "telemetry.updated",
                correlationId = Guid.NewGuid().ToString(),
                emittedAt = tickTime,
                readings = new[]
                {
                    new { sensorId = reading.SensorId, value = reading.Value, unit = reading.Unit, capturedAt = reading.CapturedAt },
                },
            };

            var mqJson = JsonSerializer.Serialize(envelope, EnvelopeJson);
            try
            {
                await mq.PublishTelemetryUpdatedAsync(mqJson, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ publish failed for sensor {SensorId} after Redis succeeded", sensorId);
            }
        }
    }
}
