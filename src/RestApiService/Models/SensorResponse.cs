namespace RestApiService.Models;

public sealed class SensorResponse
{
    public int Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public double? LatestValue { get; set; }

    public string? LatestUnit { get; set; }

    public DateTimeOffset? LatestCapturedAt { get; set; }
}
