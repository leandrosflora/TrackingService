using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using TrackingService.Contracts;
using TrackingService.Domain;

namespace TrackingService.Infrastructure.Messaging;

public sealed class KafkaTrackingMessageConsumer : ITrackingMessageConsumer, IDisposable
{
    private readonly IConsumer<string, string> _consumer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaTrackingMessageConsumer> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public KafkaTrackingMessageConsumer(IOptions<KafkaOptions> options, ILogger<KafkaTrackingMessageConsumer> logger)
    {
        _options = options.Value;
        _logger = logger;
        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            SocketTimeoutMs = 10000,
            SessionTimeoutMs = 10000
        }).Build();
    }

    public async IAsyncEnumerable<ConsumedTrackingMessage> ConsumeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _consumer.Subscribe(_options.Topics.ShipmentCreated);
        _logger.LogInformation("Kafka tracking consumer started for topic {Topic} with group {ConsumerGroupId}", _options.Topics.ShipmentCreated, _options.ConsumerGroupId);

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumedTrackingMessage? message = null;
            ConsumeResult<string, string>? result = null;
            try
            {
                result = _consumer.Consume(cancellationToken);
                message = Map(result);
                _logger.LogInformation(
                    "Consumed Kafka event from topic {Topic} with key {MessageKey}, eventType {EventType}, correlationId {CorrelationId}, partition {Partition}, offset {Offset}",
                    message.Topic,
                    result.Message.Key,
                    _options.Topics.ShipmentCreated,
                    message.CorrelationId,
                    message.Partition,
                    message.Offset);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            catch (ConsumeException exception)
            {
                _logger.LogError(exception, "Kafka consume failed for topic {Topic}", _options.Topics.ShipmentCreated);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Invalid Kafka message on topic {Topic}, partition {Partition}, offset {Offset}", result?.Topic, result?.Partition.Value, result?.Offset.Value);
                if (result is not null)
                    _consumer.Commit(result);
            }

            if (message is not null)
                yield return message;
        }
    }

    public Task CommitAsync(ConsumedTrackingMessage message, CancellationToken cancellationToken)
    {
        _consumer.Commit(new TopicPartitionOffset(message.Topic, new Partition(message.Partition), new Offset(message.Offset + 1)));
        _logger.LogDebug("Committed Kafka message {Topic}/{Partition}/{Offset}", message.Topic, message.Partition, message.Offset);
        return Task.CompletedTask;
    }

    public Task NackAsync(ConsumedTrackingMessage message, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Nacked Kafka message {Topic}/{Partition}/{Offset}; offset was not committed", message.Topic, message.Partition, message.Offset);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
    }

    private ConsumedTrackingMessage Map(ConsumeResult<string, string> result)
    {
        var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(result.Message.Value, JsonOptions)
            ?? throw new InvalidOperationException("Kafka event envelope is empty.");

        if (envelope.EventType != _options.Topics.ShipmentCreated)
            throw new InvalidOperationException($"Unsupported event type '{envelope.EventType}'.");

        var shipmentCreated = envelope.Payload.Deserialize<ShipmentCreatedIntegrationEvent>(JsonOptions)
            ?? throw new InvalidOperationException("shipment.created payload is empty.");

        var occurredAt = shipmentCreated.CreatedAt ?? envelope.OccurredAt;
        var trackingEvent = new CarrierTrackingEventIntegrationEvent(
            MessageId: envelope.EventId,
            CorrelationId: envelope.CorrelationId,
            ProviderEventId: $"shipment.created:{envelope.EventId:N}",
            ShipmentId: shipmentCreated.ShipmentId,
            TrackingCode: shipmentCreated.TrackingCode,
            CarrierCode: shipmentCreated.CarrierCode,
            CarrierSequence: 0,
            Status: TrackingStatus.Created,
            Description: "Shipment created.",
            ExceptionCode: null,
            Location: null,
            OccurredAt: occurredAt,
            ReceivedAt: DateTimeOffset.UtcNow,
            EstimatedDeliveryDate: shipmentCreated.EstimatedDeliveryDate);

        return new ConsumedTrackingMessage(
            result.Topic,
            result.Partition.Value,
            result.Offset.Value,
            envelope.CorrelationId,
            trackingEvent);
    }
}
