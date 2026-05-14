using Grpc.Core;
using Grpc.Net.Client;
using Industrial.Sqldata.V1;
using Microsoft.AspNetCore.Http.HttpResults;
using RestApiService.Configuration;
using RestApiService.Messaging;
using RestApiService.Models;
using RestApiService.Redis;
using RestApiService.Services;
using RestApiService.SignalR;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var isTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is required.");
if (isTesting && !redisConnection.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
    redisConnection += ",abortConnect=false,connectTimeout=500";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<ISensorLatestRedisReader, SensorLatestRedisReader>();

var sqlDataAddress = builder.Configuration["SqlDataGrpc:Address"]
    ?? throw new InvalidOperationException("SqlDataGrpc:Address is required.");
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
builder.Services.AddSingleton(_ => GrpcChannel.ForAddress(sqlDataAddress));
builder.Services.AddSingleton(sp =>
    new SensorTelemetry.SensorTelemetryClient(sp.GetRequiredService<GrpcChannel>()));

builder.Services.AddSignalR();
if (!isTesting)
    builder.Services.AddHostedService<TelemetryUpdatedConsumer>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(_ => true)));

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/sensors", async Task<Ok<List<SensorResponse>>> (
    SensorTelemetry.SensorTelemetryClient grpc,
    ISensorLatestRedisReader redis,
    CancellationToken cancellationToken) =>
{
    var list = await grpc.GetSensorsAsync(new GetSensorsRequest(), cancellationToken: cancellationToken).ConfigureAwait(false);
    var merged = new List<SensorResponse>(list.Sensors.Count);
    foreach (var s in list.Sensors)
    {
        var latest = await redis.TryGetLatestAsync(s.Id, cancellationToken).ConfigureAwait(false);
        merged.Add(SensorResponseMapper.Merge(s, latest));
    }

    return TypedResults.Ok(merged);
});

app.MapGet("/api/sensors/{id:int}", async Task<Results<Ok<SensorResponse>, NotFound>> (
    int id,
    SensorTelemetry.SensorTelemetryClient grpc,
    ISensorLatestRedisReader redis,
    CancellationToken cancellationToken) =>
{
    var r = await grpc.GetSensorByIdAsync(new GetSensorByIdRequest { SensorId = id }, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
    if (!r.Found)
        return TypedResults.NotFound();

    var latest = await redis.TryGetLatestAsync(id, cancellationToken).ConfigureAwait(false);
    return TypedResults.Ok(SensorResponseMapper.Merge(r.Sensor, latest));
});

app.MapGet("/api/sensors/{id:int}/history", async (
    int id,
    SensorTelemetry.SensorTelemetryClient grpc,
    int? pageSize,
    string? pageToken,
    DateTimeOffset? from,
    DateTimeOffset? to,
    CancellationToken cancellationToken) =>
{
    var req = new GetTelemetryHistoryRequest
    {
        SensorId = id,
        PageSize = pageSize ?? 0,
        PageToken = pageToken ?? string.Empty,
    };
    if (from is not null)
        req.FromUtc = SensorResponseMapper.ToProtoTimestamp(from.Value);
    if (to is not null)
        req.ToUtc = SensorResponseMapper.ToProtoTimestamp(to.Value);

    var hist = await grpc.GetTelemetryHistoryAsync(req, cancellationToken: cancellationToken).ConfigureAwait(false);
    return Results.Ok(new
    {
        items = hist.Items.Select(i => new
        {
            i.Id,
            i.SensorId,
            i.Value,
            i.Unit,
            capturedAt = i.CapturedAt.ToDateTimeOffset(),
        }),
        nextPageToken = hist.NextPageToken,
    });
});

app.MapGet("/api/system/status", async (IConnectionMultiplexer redis, SensorTelemetry.SensorTelemetryClient grpc) =>
{
    var redisOk = false;
    try
    {
        await redis.GetDatabase().PingAsync().ConfigureAwait(false);
        redisOk = true;
    }
    catch (Exception)
    {
        // structured failure surfaced in payload
    }

    var grpcOk = false;
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await grpc.GetSensorsAsync(new GetSensorsRequest(), new CallOptions(cancellationToken: cts.Token)).ConfigureAwait(false);
        grpcOk = true;
    }
    catch (Exception)
    {
    }

    return Results.Ok(new
    {
        redis = redisOk ? "ok" : "unavailable",
        sqlDataGrpc = grpcOk ? "ok" : "unavailable",
        timestampUtc = DateTimeOffset.UtcNow,
    });
});

app.MapHub<TelemetryHub>("/hubs/telemetry");

app.Run();

public partial class Program { }
