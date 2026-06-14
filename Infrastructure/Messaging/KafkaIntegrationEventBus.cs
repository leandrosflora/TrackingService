using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace TrackingService.Infrastructure.Messaging;

public sealed class KafkaIntegrationEventBus : IIntegrationEventBus, IDisposable
{
    private const string ProducerName = "tracking-service";
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaIntegrationEventBus> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public KafkaIntegrationEventBus(IOptions<KafkaOptions> options, ILogger<KafkaIntegrationEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 10000,
            SocketTimeoutMs = 10000
        }).Build();
    }

    public async Task PublishAsync(string topic, string aggregateKey, string messageType, string payload, CancellationToken cancellationToken)
    {
        if (topic != _options.Topics.ShipmentStatusUpdated)
            throw new InvalidOperationException($"Kafka topic '{topic}' is not configured for TrackingService publication.");

        var eventType = ResolveEventType(topic, messageType);
        var payloadElement = JsonSerializer.Deserialize<JsonElement>(payload, JsonOptions);
        var correlationId = TryGetCorrelationId(payloadElement) ?? Guid.NewGuid().ToString("N");
        var eventId = TryGetMessageId(payloadElement) ?? Guid.NewGuid();
        var occurredAt = TryGetOccurredAt(payloadElement) ?? DateTimeOffset.UtcNow;
        var envelope = new IntegrationEventEnvelope(
            eventId,
            eventType,
            "1.0",
            occurredAt,
            correlationId,
            ProducerName,
            payloadElement);

        var value = JsonSerializer.Serialize(envelope, JsonOptions);

        try
        {
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = aggregateKey,
                Value = value,
                Headers = new Headers
                {
                    { "eventType", System.Text.Encoding.UTF8.GetBytes(eventType) },
                    { "correlationId", System.Text.Encoding.UTF8.GetBytes(correlationId) }
                }
            }, cancellationToken);

            _logger.LogInformation(
                "Published Kafka event to topic {Topic} with key {MessageKey}, eventType {EventType}, correlationId {CorrelationId}, partition {Partition}, offset {Offset}",
                result.Topic,
                aggregateKey,
                eventType,
                correlationId,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> exception)
        {
            _logger.LogError(exception, "Kafka publish failed for topic {Topic}, key {MessageKey}, eventType {EventType}, correlationId {CorrelationId}", topic, aggregateKey, eventType, correlationId);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }

    private string ResolveEventType(string topic, string messageType)
    {
        if (topic == _options.Topics.ShipmentStatusUpdated)
            return _options.Topics.ShipmentStatusUpdated;

        return messageType;
    }

    private static Guid? TryGetMessageId(JsonElement payload)
    {
        if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("messageId", out var messageId) && messageId.TryGetGuid(out var value))
            return value;
        return null;
    }

    private static DateTimeOffset? TryGetOccurredAt(JsonElement payload)
    {
        if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("occurredAt", out var occurredAt) && occurredAt.TryGetDateTimeOffset(out var value))
            return value;
        return null;
    }

    private static string? TryGetCorrelationId(JsonElement payload)
    {
        if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("correlationId", out var correlationId))
            return correlationId.GetString();
        return null;
    }
}
