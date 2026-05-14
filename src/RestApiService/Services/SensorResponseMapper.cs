using Google.Protobuf.WellKnownTypes;
using Industrial.Sqldata.V1;
using RestApiService.Models;

namespace RestApiService.Services;

public static class SensorResponseMapper
{
    public static SensorResponse Merge(Sensor sql, SensorLatestDto? latest)
    {
        return new SensorResponse
        {
            Id = sql.Id,
            DisplayName = sql.DisplayName,
            Unit = sql.Unit,
            Enabled = sql.Enabled,
            LatestValue = latest?.Value,
            LatestUnit = string.IsNullOrEmpty(latest?.Unit) ? null : latest!.Unit,
            LatestCapturedAt = latest?.CapturedAt,
        };
    }

    public static Timestamp ToProtoTimestamp(DateTimeOffset dto) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(dto.UtcDateTime, DateTimeKind.Utc));
}
