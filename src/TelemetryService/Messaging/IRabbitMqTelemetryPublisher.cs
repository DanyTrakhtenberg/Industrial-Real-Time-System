namespace TelemetryService.Messaging;

public interface IRabbitMqTelemetryPublisher
{
    Task PublishTelemetryUpdatedAsync(string jsonBody, CancellationToken cancellationToken = default);
}
