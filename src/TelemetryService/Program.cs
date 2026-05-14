using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TelemetryService.Messaging;
using TelemetryService.Redis;
using TelemetryService.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is required.");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddSingleton<IRedisLatestTelemetryWriter, RedisLatestTelemetryWriter>();
builder.Services.AddSingleton<IRabbitMqTelemetryPublisher, RabbitMqTelemetryPublisher>();
builder.Services.AddSingleton<ISensorValueGenerator, SinusoidalSensorValueGenerator>();
builder.Services.AddSingleton<TelemetryCycleExecutor>();
builder.Services.AddHostedService<TelemetrySimulatorHostedService>();

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { }
