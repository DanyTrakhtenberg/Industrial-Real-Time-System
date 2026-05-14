extern alias sql;
extern alias rest;

using System.Collections.Concurrent;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Industrial.Sqldata.V1;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TelemetryService.Messaging;
using TelemetryService.Redis;
using TelemetryService.Telemetry;

namespace IntegrationTests;

[Collection("Industrial integration")]
public sealed class TelemetryPipelineIntegrationTests(IndustrialIntegrationFixture fx)
{
    [Fact]
    public async Task Telemetry_cycle_writes_latest_redis_keys_for_all_twenty_sensors()
    {
        await using var mq = CreatePublisher(fx);
        await using var redisMux = await ConnectionMultiplexer.ConnectAsync(fx.RedisConnectionString);
        var executor = new TelemetryCycleExecutor(
            new RedisLatestTelemetryWriter(redisMux, NullLogger<RedisLatestTelemetryWriter>.Instance),
            mq,
            new SinusoidalSensorValueGenerator(),
            NullLogger<TelemetryCycleExecutor>.Instance);

        await executor.RunOnceAsync(new DateTimeOffset(2026, 5, 14, 12, 0, 0, TimeSpan.Zero));

        var db = redisMux.GetDatabase();
        for (var id = 1; id <= 20; id++)
        {
            var key = $"sensor:{id}:latest";
            var val = await db.StringGetAsync(key);
            Assert.False(val.IsNullOrEmpty, $"Missing Redis key {key}");
            using var doc = JsonDocument.Parse(val.ToString()!);
            Assert.Equal(id, doc.RootElement.GetProperty("sensorId").GetInt32());
        }
    }

    [Fact]
    public async Task SignalR_receives_telemetryUpdated_for_all_twenty_sensors_after_one_cycle()
    {
        var hubUri = new UriBuilder(fx.RestFactory.Server.BaseAddress)
        {
            Path = "/hubs/sensor-stream",
        }.Uri;

        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUri, o =>
            {
                o.HttpMessageHandlerFactory = _ => fx.RestFactory.Server.CreateHandler();
                o.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling |
                               Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
            })
            .Build();

        var seenSensorIds = new ConcurrentDictionary<int, byte>();
        connection.On<JsonElement>("telemetryUpdated", payload =>
        {
            if (!payload.TryGetProperty("readings", out var readings))
                return;
            foreach (var r in readings.EnumerateArray())
            {
                if (r.TryGetProperty("sensorId", out var sid))
                    seenSensorIds[sid.GetInt32()] = 0;
            }
        });

        await connection.StartAsync();

        await using var mq = CreatePublisher(fx);
        await using var redisMux = await ConnectionMultiplexer.ConnectAsync(fx.RedisConnectionString);
        var executor = new TelemetryCycleExecutor(
            new RedisLatestTelemetryWriter(redisMux, NullLogger<RedisLatestTelemetryWriter>.Instance),
            mq,
            new SinusoidalSensorValueGenerator(),
            NullLogger<TelemetryCycleExecutor>.Instance);

        await executor.RunOnceAsync(new DateTimeOffset(2026, 5, 14, 12, 1, 0, TimeSpan.Zero));

        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (seenSensorIds.Count < 20 && DateTime.UtcNow < deadline)
            await Task.Delay(100);

        Assert.Equal(20, seenSensorIds.Count);
        Assert.Equal(Enumerable.Range(1, 20).ToHashSet(), seenSensorIds.Keys.ToHashSet());
    }

    [Fact]
    public async Task RestApi_GetSensors_returns_twenty_rows_with_latest_from_redis_after_cycle()
    {
        await using var mq = CreatePublisher(fx);
        await using var redisMux = await ConnectionMultiplexer.ConnectAsync(fx.RedisConnectionString);
        var executor = new TelemetryCycleExecutor(
            new RedisLatestTelemetryWriter(redisMux, NullLogger<RedisLatestTelemetryWriter>.Instance),
            mq,
            new SinusoidalSensorValueGenerator(),
            NullLogger<TelemetryCycleExecutor>.Instance);

        await executor.RunOnceAsync(new DateTimeOffset(2026, 5, 14, 12, 2, 0, TimeSpan.Zero));

        var client = fx.RestFactory.CreateClient();
        var response = await client.GetAsync("/api/sensors");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(20, root.GetArrayLength());
        foreach (var el in root.EnumerateArray())
        {
            Assert.True(el.TryGetProperty("latestValue", out var lv) && lv.ValueKind == JsonValueKind.Number);
        }
    }

    [Fact]
    public async Task SqlData_persists_all_twenty_sensor_readings_via_consumer_after_one_cycle()
    {
        await using var mq = CreatePublisher(fx);
        await using var redisMux = await ConnectionMultiplexer.ConnectAsync(fx.RedisConnectionString);
        var executor = new TelemetryCycleExecutor(
            new RedisLatestTelemetryWriter(redisMux, NullLogger<RedisLatestTelemetryWriter>.Instance),
            mq,
            new SinusoidalSensorValueGenerator(),
            NullLogger<TelemetryCycleExecutor>.Instance);

        await executor.RunOnceAsync(new DateTimeOffset(2026, 5, 14, 12, 3, 0, TimeSpan.Zero));

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        using var channel = GrpcChannel.ForAddress(
            fx.SqlFactory.Server.BaseAddress,
            new GrpcChannelOptions { HttpHandler = fx.SqlFactory.Server.CreateHandler() });
        var grpc = new SensorTelemetry.SensorTelemetryClient(channel);

        for (var id = 1; id <= 20; id++)
        {
            var found = false;
            var sensorDeadline = DateTime.UtcNow.AddSeconds(45);
            while (DateTime.UtcNow < sensorDeadline)
            {
                var hist = await grpc.GetTelemetryHistoryAsync(
                    new GetTelemetryHistoryRequest { SensorId = id, PageSize = 10 },
                    new CallOptions(deadline: DateTime.UtcNow.AddSeconds(10)));

                if (hist.Items.Count > 0)
                {
                    found = true;
                    break;
                }

                await Task.Delay(150);
            }

            Assert.True(found, $"Expected telemetry persisted for sensor {id}");
        }
    }

    private static RabbitMqTelemetryPublisher CreatePublisher(IndustrialIntegrationFixture fx)
    {
        var opt = Options.Create(
            new RabbitMqOptions
            {
                HostName = fx.RabbitHost,
                Port = fx.RabbitPort,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                Exchange = "industrial.topic",
                TelemetryRoutingKey = "telemetry.updated",
            });
        return new RabbitMqTelemetryPublisher(opt, NullLogger<RabbitMqTelemetryPublisher>.Instance);
    }
}
