namespace TrackingService.Infrastructure.Messaging;

public sealed class KafkaIntegrationEventBus : IIntegrationEventBus
{
    private readonly ILogger<KafkaIntegrationEventBus> _logger;

    public KafkaIntegrationEventBus(ILogger<KafkaIntegrationEventBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(string topic, string aggregateKey, string messageType, string payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Publishing integration event {MessageType} to {Topic} with aggregate key {AggregateKey}",
            messageType,
            topic,
            aggregateKey);

        return Task.CompletedTask;
    }
}
