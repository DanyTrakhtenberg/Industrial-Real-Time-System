using Microsoft.EntityFrameworkCore;
using SqlDataService.Data;
using SqlDataService.Data.Entities;

namespace SqlDataService.Tests.TestSupport;

/// <summary>
/// Deterministic test data for <see cref="ApplicationDbContext"/>.
/// </summary>
public static class TestDataSeed
{
    /// <summary>Seeds sensors with ids 1..20 if the table is empty.</summary>
    public static async Task SeedTwentySensorsAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Sensors.AnyAsync(cancellationToken))
            return;

        for (var i = 1; i <= 20; i++)
        {
            db.Sensors.Add(new Sensor
            {
                Id = i,
                DisplayName = $"Sensor {i}",
                Unit = string.Empty,
                Enabled = true,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Adds a telemetry row. Caller must ensure <paramref name="sensorId"/> exists.</summary>
    public static async Task<long> AddTelemetryReadingAsync(
        ApplicationDbContext db,
        int sensorId,
        double value,
        DateTime capturedAtUtc,
        string unit = "",
        CancellationToken cancellationToken = default)
    {
        var utc = DateTime.SpecifyKind(capturedAtUtc, DateTimeKind.Utc);
        var row = new TelemetryReading
        {
            SensorId = sensorId,
            Value = value,
            Unit = string.IsNullOrEmpty(unit) ? string.Empty : unit,
            CapturedAtUtc = utc,
        };

        db.TelemetryReadings.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return row.Id;
    }
}
