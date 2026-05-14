namespace SqlDataService.Data.Entities;

public sealed class Sensor
{
    public int Id { get; set; }

    public required string DisplayName { get; set; }

    public required string Unit { get; set; }

    public bool Enabled { get; set; } = true;

    public ICollection<TelemetryReading> Readings { get; } = new List<TelemetryReading>();
}
