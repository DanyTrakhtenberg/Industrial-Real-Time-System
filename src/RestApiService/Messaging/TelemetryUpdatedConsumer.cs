using System.Text;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Industrial.Sqldata.V1;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RestApiService.Configuration;
using RestApiService.Models;
using RestApiService.Services;
using RestApiService.SignalR;

namespace RestApiService.Messaging;

public sealed class TelemetryUpdatedConsumer(
    IOptions<RabbitMqOptions> options,
    SensorTelemetry.SensorTelemetryClient sqlData,
    IHubContext<TelemetryHub> hub,
    ILogger<TelemetryUpdatedConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RabbitMqOptions _opt = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _opt.HostName,
            Port = _opt.Port,
            UserName = _opt.UserName,
            Password = _opt.Password,
            VirtualHost = _opt.VirtualHost,
            DispatchConsumersAsync = true,
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? connection = null;
            IModel? channel = null;
            try
            {
                connection = factory.CreateConnection();
                channel = connection.CreateModel();
                channel.ExchangeDeclare(_opt.Exchange, ExchangeType.Topic, durable: true);
                channel.QueueDeclare(_opt.TelemetryQueue, durable: true, exclusive: false, autoDelete: false);
                channel.QueueBind(_opt.TelemetryQueue, _opt.Exchange, _opt.TelemetryRoutingKey);
                channel.BasicQos(0, prefetchCount: 32, global: false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += (_, ea) => HandleMessageAsync(channel, ea, stoppingToken);

                channel.BasicConsume(_opt.TelemetryQueue, autoAck: false, consumer);

                logger.LogInformation(
                    "RabbitMQ consumer started queue {Queue} exchange {Exchange} routingKey {RoutingKey}",
                    _opt.TelemetryQueue,
                    _opt.Exchange,
                    _opt.TelemetryRoutingKey);

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ consumer disconnected; retrying in 5s");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                channel?.Dispose();
                connection?.Dispose();
            }
        }
    }

    private async Task HandleMessageAsync(IModel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        var deliveryTag = ea.DeliveryTag;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var envelope = JsonSerializer.Deserialize<TelemetryUpdatedEnvelope>(json, Json);
            if (envelope?.Readings is null || envelope.Readings.Count == 0)
            {
                logger.LogWarning("Ignoring telemetry message with no readings (delivery {DeliveryTag})", deliveryTag);
                channel.BasicAck(deliveryTag, multiple: false);
                return;
            }

            foreach (var r in envelope.Readings)
            {
                stoppingToken.ThrowIfCancellationRequested();
                var req = new SaveTelemetryRequest
                {
                    SensorId = r.SensorId,
                    Value = r.Value,
                    Unit = r.Unit ?? string.Empty,
                    CapturedAt = SensorResponseMapper.ToProtoTimestamp(r.CapturedAt),
                };

                await sqlData.SaveTelemetryAsync(req, cancellationToken: stoppingToken).ConfigureAwait(false);
            }

            await hub.Clients.All
                .SendAsync("telemetryUpdated", envelope, stoppingToken)
                .ConfigureAwait(false);

            channel.BasicAck(deliveryTag, multiple: false);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid telemetry JSON; ack without requeue (delivery {DeliveryTag})", deliveryTag);
            channel.BasicAck(deliveryTag, multiple: false);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            logger.LogWarning(ex, "SaveTelemetry returned NotFound; acking message (delivery {DeliveryTag})", deliveryTag);
            channel.BasicAck(deliveryTag, multiple: false);
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "gRPC error persisting telemetry; nack with requeue (delivery {DeliveryTag})", deliveryTag);
            channel.BasicNack(deliveryTag, multiple: false, requeue: true);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            channel.BasicNack(deliveryTag, multiple: false, requeue: true);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error handling telemetry; nack with requeue (delivery {DeliveryTag})", deliveryTag);
            channel.BasicNack(deliveryTag, multiple: false, requeue: true);
        }
    }
}
