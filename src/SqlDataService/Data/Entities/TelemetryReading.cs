namespace SqlDataService.Data.Entities;

public sealed class TelemetryReading
{
    public long Id { get; set; }

    public int SensorId { get; set; }

    public Sensor Sensor { get; set; } = null!;

    public double Value { get; set; }

    public required string Unit { get; set; }

    public DateTime CapturedAtUtc { get; set; }
}
