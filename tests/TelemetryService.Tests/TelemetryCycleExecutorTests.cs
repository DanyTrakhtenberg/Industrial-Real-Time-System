using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TelemetryService.Messaging;
using TelemetryService.Redis;
using TelemetryService.Telemetry;

namespace TelemetryService.Tests;

public sealed class FakeRedisWriter : IRedisLatestTelemetryWriter
{
    public List<(int SensorId, string Json)> Writes { get; } = new();

    public Func<int, bool>? ShouldSucceed { get; set; }

    public Task<bool> TryWriteLatestJsonAsync(int sensorId, string json, CancellationToken cancellationToken = default)
    {
        if (ShouldSucceed is not null && !ShouldSucceed(sensorId))
            return Task.FromResult(false);

        Writes.Add((sensorId, json));
        return Task.FromResult(true);
    }
}

public sealed class FakeMqPublisher : IRabbitMqTelemetryPublisher
{
    public List<string> Messages { get; } = new();

    public Task PublishTelemetryUpdatedAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        Messages.Add(jsonBody);
        return Task.CompletedTask;
    }
}

public sealed class TelemetryCycleExecutorTests
{
    [Fact]
    public async Task RunOnceAsync_writes_twenty_redis_keys_and_publishes_twenty_messages_when_redis_always_succeeds()
    {
        var redis = new FakeRedisWriter();
        var mq = new FakeMqPublisher();
        var gen = new SinusoidalSensorValueGenerator();
        var executor = new TelemetryCycleExecutor(redis, mq, gen, NullLogger<TelemetryCycleExecutor>.Instance);
        var tick = new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

        await executor.RunOnceAsync(tick, CancellationToken.None);

        Assert.Equal(20, redis.Writes.Count);
        Assert.Equal(Enumerable.Range(1, 20), redis.Writes.Select(w => w.SensorId));
        Assert.Equal(20, mq.Messages.Count);
    }

    [Fact]
    public async Task RunOnceAsync_does_not_publish_to_rabbit_when_redis_write_fails_for_that_sensor()
    {
        var redis = new FakeRedisWriter { ShouldSucceed = id => id != 7 };
        var mq = new FakeMqPublisher();
        var gen = new SinusoidalSensorValueGenerator();
        var executor = new TelemetryCycleExecutor(redis, mq, gen, NullLogger<TelemetryCycleExecutor>.Instance);
        var tick = new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

        await executor.RunOnceAsync(tick, CancellationToken.None);

        Assert.Equal(19, redis.Writes.Count);
        Assert.DoesNotContain(redis.Writes, w => w.SensorId == 7);
        Assert.Equal(19, mq.Messages.Count);
        foreach (var payload in mq.Messages)
        {
            using var doc = JsonDocument.Parse(payload);
            var readings = doc.RootElement.GetProperty("readings");
            var sid = readings[0].GetProperty("sensorId").GetInt32();
            Assert.NotEqual(7, sid);
        }
    }

    [Fact]
    public async Task RunOnceAsync_redis_json_matches_sensor_key_contract()
    {
        var redis = new FakeRedisWriter();
        var mq = new FakeMqPublisher();
        var gen = new SinusoidalSensorValueGenerator();
        var executor = new TelemetryCycleExecutor(redis, mq, gen, NullLogger<TelemetryCycleExecutor>.Instance);
        var tick = new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

        await executor.RunOnceAsync(tick, CancellationToken.None);

        var (_, json) = Assert.Single(redis.Writes, w => w.SensorId == 1);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("sensorId").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("value", out _));
        Assert.True(doc.RootElement.TryGetProperty("capturedAt", out _));
    }

    [Fact]
    public void SinusoidalSensorValueGenerator_is_deterministic_per_sensor_and_tick()
    {
        var gen = new SinusoidalSensorValueGenerator();
        var t = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var a = gen.CreateReading(4, t);
        var b = gen.CreateReading(4, t);
        Assert.Equal(a.Value, b.Value);
        Assert.NotEqual(gen.CreateReading(4, t).Value, gen.CreateReading(5, t).Value);
    }
}
