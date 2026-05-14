using Industrial.Sqldata.V1;
using RestApiService.Models;
using RestApiService.Services;

namespace RestApiService.Tests;

public sealed class SensorResponseMapperTests
{
    [Fact]
    public void Merge_maps_sql_fields_and_null_latest()
    {
        var sql = new Sensor { Id = 2, DisplayName = "S2", Unit = "u", Enabled = true };

        var dto = SensorResponseMapper.Merge(sql, null);

        Assert.Equal(2, dto.Id);
        Assert.Equal("S2", dto.DisplayName);
        Assert.Equal("u", dto.Unit);
        Assert.True(dto.Enabled);
        Assert.Null(dto.LatestValue);
        Assert.Null(dto.LatestCapturedAt);
    }

    [Fact]
    public void Merge_overlays_redis_latest()
    {
        var sql = new Sensor { Id = 1, DisplayName = "S1", Unit = "", Enabled = true };
        var latest = new SensorLatestDto
        {
            SensorId = 1,
            Value = 3.5,
            Unit = "x",
            CapturedAt = new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero),
        };

        var dto = SensorResponseMapper.Merge(sql, latest);

        Assert.Equal(3.5, dto.LatestValue);
        Assert.Equal("x", dto.LatestUnit);
        Assert.Equal(latest.CapturedAt, dto.LatestCapturedAt);
    }
}
