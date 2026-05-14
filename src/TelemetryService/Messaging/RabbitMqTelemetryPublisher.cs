using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TelemetryService.Messaging;

public sealed class RabbitMqTelemetryPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqTelemetryPublisher> logger) : IRabbitMqTelemetryPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _opt = options.Value;
    private readonly object _sync = new();
    private IConnection? _connection;
    private IModel? _channel;

    public Task PublishTelemetryUpdatedAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureChannel();
        var body = Encoding.UTF8.GetBytes(jsonBody);
        var props = _channel!.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2;

        _channel.BasicPublish(
            exchange: _opt.Exchange,
            routingKey: _opt.TelemetryRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body);

        logger.LogDebug(
            "Published RabbitMQ message exchange {Exchange} routingKey {RoutingKey} bytes {ByteLength}",
            _opt.Exchange,
            _opt.TelemetryRoutingKey,
            body.Length);

        return Task.CompletedTask;
    }

    private void EnsureChannel()
    {
        lock (_sync)
        {
            if (_channel is { IsOpen: true })
                return;

            _channel?.Dispose();
            _connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = _opt.HostName,
                Port = _opt.Port,
                UserName = _opt.UserName,
                Password = _opt.Password,
                VirtualHost = _opt.VirtualHost,
                DispatchConsumersAsync = true,
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(
                exchange: _opt.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            logger.LogInformation(
                "RabbitMQ connected host {HostName} port {Port} exchange {Exchange}",
                _opt.HostName,
                _opt.Port,
                _opt.Exchange);
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            _channel?.Dispose();
            _channel = null;
            _connection?.Dispose();
            _connection = null;
        }

        return ValueTask.CompletedTask;
    }
}
