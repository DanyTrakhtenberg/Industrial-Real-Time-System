namespace TelemetryService.Telemetry;

/// <summary>Runs <see cref="TelemetryCycleExecutor"/> once per wall-clock second (aligned to second boundaries).</summary>
public sealed class TelemetrySimulatorHostedService(
    TelemetryCycleExecutor executor,
    ILogger<TelemetrySimulatorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextTick = AlignToNextSecond(DateTimeOffset.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            while (nextTick <= DateTimeOffset.UtcNow && !stoppingToken.IsCancellationRequested)
                nextTick = nextTick.AddSeconds(1);

            var delay = nextTick - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            try
            {
                await executor.RunOnceAsync(nextTick, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Telemetry cycle failed at {TickTime:o}", nextTick);
            }

            nextTick = nextTick.AddSeconds(1);
        }
    }

    private static DateTimeOffset AlignToNextSecond(DateTimeOffset now)
    {
        var t = now.AddTicks(-now.Ticks % TimeSpan.TicksPerSecond).AddSeconds(1);
        return t;
    }
}
