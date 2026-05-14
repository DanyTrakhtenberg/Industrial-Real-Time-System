using Grpc.Net.Client;
using Industrial.Sqldata.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SqlDataService.Tests;

public sealed class SqlDataWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Testing");
}

public sealed class SensorTelemetryGrpcTests : IClassFixture<SqlDataWebApplicationFactory>
{
    private readonly SqlDataWebApplicationFactory _factory;

    public SensorTelemetryGrpcTests(SqlDataWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetSensors_returns_twenty_seeded_sensors()
    {
        using var channel = GrpcChannel.ForAddress(_factory.Server.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = _factory.Server.CreateHandler(),
        });
        var client = new SensorTelemetry.SensorTelemetryClient(channel);

        var response = await client.GetSensorsAsync(new GetSensorsRequest());

        Assert.Equal(20, response.Sensors.Count);
        Assert.Equal(Enumerable.Range(1, 20), response.Sensors.Select(s => s.Id));
    }

    [Fact]
    public async Task GetSensorById_returns_sensor_when_present()
    {
        using var channel = GrpcChannel.ForAddress(_factory.Server.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = _factory.Server.CreateHandler(),
        });
        var client = new SensorTelemetry.SensorTelemetryClient(channel);

        var response = await client.GetSensorByIdAsync(new GetSensorByIdRequest { SensorId = 3 });

        Assert.True(response.Found);
        Assert.Equal(3, response.Sensor.Id);
        Assert.Equal("Sensor 3", response.Sensor.DisplayName);
    }

    [Fact]
    public async Task SaveTelemetry_and_GetTelemetryHistory_roundtrip()
    {
        using var channel = GrpcChannel.ForAddress(_factory.Server.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = _factory.Server.CreateHandler(),
        });
        var client = new SensorTelemetry.SensorTelemetryClient(channel);

        var captured = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
        var save = await client.SaveTelemetryAsync(new SaveTelemetryRequest
        {
            SensorId = 1,
            Value = 42.5,
            Unit = "u",
            CapturedAt = captured,
        });

        Assert.True(save.TelemetryReadingId > 0);

        var history = await client.GetTelemetryHistoryAsync(new GetTelemetryHistoryRequest
        {
            SensorId = 1,
            FromUtc = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-5)),
            ToUtc = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5)),
            PageSize = 10,
        });

        Assert.Contains(history.Items, i => i.SensorId == 1 && Math.Abs(i.Value - 42.5) < 0.001);
    }
}
