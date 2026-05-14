using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Industrial.Sqldata.V1;
using Microsoft.EntityFrameworkCore;
using SqlDataService.Data;
using SqlDataService.Grpc;
using SqlDataService.Tests.TestSupport;

namespace SqlDataService.Tests;

public sealed class SensorTelemetryGrpcServiceTests
{
    /// <summary>Fixed UTC anchor so history windows and ordering stay deterministic.</summary>
    private static readonly DateTime UtcAnchor = new(2026, 5, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetSensors_returns_seeded_sensors_ordered_by_Id()
    {
        await using var db = TestDbContextFactory.Create();
        await TestDataSeed.SeedTwentySensorsAsync(db);
        var sut = new SensorTelemetryGrpcService(db);
        var ctx = TestGrpcCallContext.Create();

        var response = await sut.GetSensors(new GetSensorsRequest(), ctx);

        Assert.Equal(20, response.Sensors.Count);
        Assert.Equal(Enumerable.Range(1, 20), response.Sensors.Select(s => s.Id));
        Assert.Equal(Enumerable.Range(1, 20), response.Sensors.OrderBy(s => s.Id).Select(s => s.Id));
    }

    [Fact]
    public async Task GetSensorById_returns_Found_true_for_existing_sensor()
    {
        await using var db = TestDbContextFactory.Create();
        await TestDataSeed.SeedTwentySensorsAsync(db);
        var sut = new SensorTelemetryGrpcService(db);
        var ctx = TestGrpcCallContext.Create();

        var response = await sut.GetSensorById(new GetSensorByIdRequest { SensorId = 7 }, ctx);

        Assert.True(response.Found);
        Assert.Equal(7, response.Sensor.Id);
        Assert.Equal("Sensor 7", response.Sensor.DisplayName);
    }

    [Fact]
    public async Task GetSensorById_returns_Found_false_for_missing_sensor()
    {
        await using var db = TestDbContextFactory.Create();
        await TestDataSeed.SeedTwentySensorsAsync(db);
        var sut = new SensorTelemetryGrpcService(db);
        var ctx = TestGrpcCallContext.Create();

        var response = await sut.GetSensorById(new GetSensorByIdRequest { SensorId = 999 }, ctx);

        Assert.False(response.Found);
    }

    [Fact]
    public async Task SaveTelemetry_persists_row()
    {
        await using var db = TestDbContextFactory.Create();
        await TestDataSeed.SeedTwentySensorsAsync(db);
        var sut = new SensorTelemetryGrpcService(db);
        var ctx = TestGrpcCallContext.Create();

        var captured = Timestamp.FromDateTime(UtcAnchor);
        var response = await sut.SaveTelemetry(
            new SaveTelemetryRequest
            {
                SensorId = 2,
                Value = 3.25,
                Unit = "x",
                CapturedAt = captured,
            },
            ctx);

        Assert.True(response.TelemetryReadingId > 0);
        var stored = await db.TelemetryReadings.AsNoTracking().SingleAsync();
        Assert.Equal(2, stored.SensorId);
        Assert.Equal(3.25, stored.Value);
        Assert.Equal("x", stored.Unit);
        Assert.Equal(UtcAnchor, stored.CapturedAtUtc);
    }

    [Fact]
    public async Task SaveTelemetry_throws_NotFound_when_sensor_missing()
    {
        await using var db = TestDbContextFactory.Create();
        await TestDataSeed.SeedTwentySensorsAsync(db);
        var sut = new SensorTelemetryGrpcService(db);
        var ctx = TestGrpcCallContext.Create();

        var ex = await Assert.ThrowsAsync<RpcException>(() => sut.SaveTelemetry(
            new SaveTelemetryRequest
            {
                SensorId = 404,
                Value = 1,
                Unit = "",
                CapturedAt = Timestamp.FromDateTime(UtcAnchor),
            },
            ctx));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task GetTelemetryHistory_orders_newest_first()
    {
        await using var db = TestDbContextFactory.Create();
        await TestDataSeed.SeedTwentySensorsAsync(db);
        await TestDataSeed.AddTelemetryReadingAsync(db, 1, 1, UtcAnchor.AddHours(-2));
        await TestDataSeed.AddTelemetryReadingAsync(db, 1, 2, UtcAnchor.AddHours(-1));
        await TestDataSeed.AddTelemetryReadingAsync(db, 1, 3, UtcAnchor);

        var sut = new SensorTelemetryGrpcService(db);
        var ctx = TestGrpcCallContext.Create();

        var response = await sut.GetTelemetryHistory(
            new GetTelemetryHistoryRequest
            {
                SensorId = 1,
                FromUtc = Timestamp.FromDateTime(UtcAnchor.AddHours(-3)),
                ToUtc = Timestamp.FromDateTime(UtcAnchor.AddHours(1)),
                PageSize = 10,
            },
            ctx);

        Assert.Equal(3, response.Items.Count);
        Assert.Equal(new[] { 3d, 2d, 1d }, response.Items.Select(i => i.Value));
        Assert.True(response.Items[0].CapturedAt.ToDateTimeOffset().UtcDateTime >=
                    response.Items[1].CapturedAt.ToDateTimeOffset().UtcDateTime);
        Assert.True(response.Items[1].CapturedAt.ToDateTimeOffset().UtcDateTime >=
                    response.Items[2].CapturedAt.ToDateTimeOffset().UtcDateTime);
    }

    [Fact]
    public async Task GetTelemetryHistory_filters_by_SensorId()
    {
        await using var db = TestDbContextFactory.Create();
        await TestDataSeed.SeedTwentySensorsAsync(db);
        await TestDataSeed.AddTelemetryReadingAsync(db, 1, 100, UtcAnchor);
        await TestDataSeed.AddTelemetryReadingAsync(db, 2, 200, UtcAnchor);

        var sut = new SensorTelemetryGrpcService(db);
        var ctx = TestGrpcCallContext.Create();

        var response = await sut.GetTelemetryHistory(
            new GetTelemetryHistoryRequest
            {
                SensorId = 2,
                FromUtc = Timestamp.FromDateTime(UtcAnchor.AddHours(-1)),
                ToUtc = Timestamp.FromDateTime(UtcAnchor.AddHours(1)),
                PageSize = 10,
            },
            ctx);

        var single = Assert.Single(response.Items);
        Assert.Equal(2, single.SensorId);
        Assert.Equal(200, single.Value);
    }

    [Fact]
    public async Task SaveTelemetry_then_GetTelemetryHistory_with_omitted_bounds_returns_row()
    {
        await using var db = TestDbContextFactory.Create();
        await TestDataSeed.SeedTwentySensorsAsync(db);
        var sut = new SensorTelemetryGrpcService(db);
        var ctx = TestGrpcCallContext.Create();

        await sut.SaveTelemetry(
            new SaveTelemetryRequest
            {
                SensorId = 3,
                Value = 9.99,
                Unit = "psi",
                CapturedAt = Timestamp.FromDateTime(UtcAnchor),
            },
            ctx);

        var history = await sut.GetTelemetryHistory(
            new GetTelemetryHistoryRequest
            {
                SensorId = 3,
                PageSize = 20,
            },
            ctx);

        var row = Assert.Single(history.Items);
        Assert.Equal(3, row.SensorId);
        Assert.Equal(9.99, row.Value);
        Assert.Equal("psi", row.Unit);
    }

    [Fact]
    public async Task GetTelemetryHistory_pagination_with_page_token()
    {
        await using var db = TestDbContextFactory.Create();
        await TestDataSeed.SeedTwentySensorsAsync(db);
        // Increasing capture times so Ids grow with time (typical for InMemory identity).
        for (var i = 0; i < 5; i++)
            await TestDataSeed.AddTelemetryReadingAsync(db, 1, i + 1, UtcAnchor.AddMinutes(i));

        var sut = new SensorTelemetryGrpcService(db);
        var ctx = TestGrpcCallContext.Create();
        var windowFrom = Timestamp.FromDateTime(UtcAnchor.AddMinutes(-1));
        var windowTo = Timestamp.FromDateTime(UtcAnchor.AddMinutes(10));

        var page1 = await sut.GetTelemetryHistory(
            new GetTelemetryHistoryRequest
            {
                SensorId = 1,
                FromUtc = windowFrom,
                ToUtc = windowTo,
                PageSize = 2,
                PageToken = string.Empty,
            },
            ctx);

        Assert.Equal(2, page1.Items.Count);
        Assert.False(string.IsNullOrEmpty(page1.NextPageToken));

        var page2 = await sut.GetTelemetryHistory(
            new GetTelemetryHistoryRequest
            {
                SensorId = 1,
                FromUtc = windowFrom,
                ToUtc = windowTo,
                PageSize = 2,
                PageToken = page1.NextPageToken,
            },
            ctx);

        Assert.Equal(2, page2.Items.Count);
        Assert.False(string.IsNullOrEmpty(page2.NextPageToken));

        var page3 = await sut.GetTelemetryHistory(
            new GetTelemetryHistoryRequest
            {
                SensorId = 1,
                FromUtc = windowFrom,
                ToUtc = windowTo,
                PageSize = 2,
                PageToken = page2.NextPageToken,
            },
            ctx);

        Assert.Single(page3.Items);
        Assert.True(string.IsNullOrEmpty(page3.NextPageToken));

        var allIds = page1.Items.Concat(page2.Items).Concat(page3.Items).Select(i => i.Id).ToList();
        Assert.Equal(5, allIds.Distinct().Count());
    }
}
