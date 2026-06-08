namespace TrackingService.Infrastructure.Messaging;

public interface IIntegrationEventBus
{
    Task PublishAsync(string topic, string aggregateKey, string messageType, string payload, CancellationToken cancellationToken);
}
