namespace RestApiService.Models;

/// <summary>Redis payload written by TelemetryService (<c>sensor:{id}:latest</c>).</summary>
public sealed class SensorLatestDto
{
    public int SensorId { get; set; }

    public double Value { get; set; }

    public string? Unit { get; set; }

    public DateTimeOffset CapturedAt { get; set; }
}
