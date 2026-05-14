using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Industrial.Sqldata.V1;
using Microsoft.EntityFrameworkCore;
using SqlDataService.Data;
using ProtoSensor = Industrial.Sqldata.V1.Sensor;
using TelemetryRow = SqlDataService.Data.Entities.TelemetryReading;

namespace SqlDataService.Grpc;

public sealed class SensorTelemetryGrpcService(ApplicationDbContext db) : SensorTelemetry.SensorTelemetryBase
{
    private static Timestamp FromUtc(DateTime utc) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc));

    private static DateTime ToUtc(Timestamp ts) => ts.ToDateTimeOffset().UtcDateTime;

    public override async Task<GetSensorsResponse> GetSensors(
        GetSensorsRequest request,
        ServerCallContext context)
    {
        var list = await db.Sensors.AsNoTracking()
            .OrderBy(s => s.Id)
            .ToListAsync(context.CancellationToken);

        var response = new GetSensorsResponse();
        foreach (var s in list)
        {
            response.Sensors.Add(new ProtoSensor
            {
                Id = s.Id,
                DisplayName = s.DisplayName,
                Unit = s.Unit,
                Enabled = s.Enabled,
            });
        }

        return response;
    }

    public override async Task<GetSensorByIdResponse> GetSensorById(
        GetSensorByIdRequest request,
        ServerCallContext context)
    {
        var entity = await db.Sensors.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SensorId, context.CancellationToken);

        if (entity is null)
            return new GetSensorByIdResponse { Found = false };

        return new GetSensorByIdResponse
        {
            Found = true,
            Sensor = new ProtoSensor
            {
                Id = entity.Id,
                DisplayName = entity.DisplayName,
                Unit = entity.Unit,
                Enabled = entity.Enabled,
            },
        };
    }

    public override async Task<SaveTelemetryResponse> SaveTelemetry(
        SaveTelemetryRequest request,
        ServerCallContext context)
    {
        var exists = await db.Sensors.AnyAsync(s => s.Id == request.SensorId, context.CancellationToken);
        if (!exists)
            throw new RpcException(new Status(StatusCode.NotFound, $"Sensor {request.SensorId} was not found."));

        if (request.CapturedAt is null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "captured_at is required."));

        var capturedAt = ToUtc(request.CapturedAt);
        var row = new TelemetryRow
        {
            SensorId = request.SensorId,
            Value = request.Value,
            Unit = string.IsNullOrEmpty(request.Unit) ? string.Empty : request.Unit,
            CapturedAtUtc = capturedAt,
        };

        db.TelemetryReadings.Add(row);
        await db.SaveChangesAsync(context.CancellationToken);

        return new SaveTelemetryResponse { TelemetryReadingId = row.Id };
    }

    public override async Task<GetTelemetryHistoryResponse> GetTelemetryHistory(
        GetTelemetryHistoryRequest request,
        ServerCallContext context)
    {
        var now = DateTime.UtcNow;
        // Well-known Timestamp fields are null when omitted on the wire (proto3 optional message fields).
        var from = request.FromUtc is null || (request.FromUtc.Seconds == 0 && request.FromUtc.Nanos == 0)
            ? now.AddDays(-7)
            : ToUtc(request.FromUtc);
        var to = request.ToUtc is null || (request.ToUtc.Seconds == 0 && request.ToUtc.Nanos == 0)
            ? now
            : ToUtc(request.ToUtc);
        var pageSize = request.PageSize is > 0 and <= 500 ? request.PageSize : 50;

        var query = db.TelemetryReadings.AsNoTracking().Where(t => t.CapturedAtUtc >= from && t.CapturedAtUtc <= to);

        if (request.SensorId > 0)
            query = query.Where(t => t.SensorId == request.SensorId);

        if (long.TryParse(request.PageToken, out var cursorId) && cursorId > 0)
            query = query.Where(t => t.Id < cursorId);

        var take = pageSize + 1;
        var rows = await query
            .OrderByDescending(t => t.CapturedAtUtc)
            .ThenByDescending(t => t.Id)
            .Take(take)
            .ToListAsync(context.CancellationToken);

        string? nextToken = null;
        if (rows.Count > pageSize)
        {
            rows = rows.Take(pageSize).ToList();
            nextToken = rows[^1].Id.ToString();
        }

        var response = new GetTelemetryHistoryResponse();
        foreach (var t in rows)
        {
            response.Items.Add(new TelemetryHistoryItem
            {
                Id = t.Id,
                SensorId = t.SensorId,
                Value = t.Value,
                Unit = t.Unit,
                CapturedAt = FromUtc(t.CapturedAtUtc),
            });
        }

        if (nextToken is not null)
            response.NextPageToken = nextToken;

        return response;
    }
}
