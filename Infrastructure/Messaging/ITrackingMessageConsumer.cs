using TrackingService.Contracts;

namespace TrackingService.Infrastructure.Messaging;

public interface ITrackingMessageConsumer
{
    IAsyncEnumerable<ConsumedTrackingMessage> ConsumeAsync(CancellationToken cancellationToken);
    Task CommitAsync(ConsumedTrackingMessage message, CancellationToken cancellationToken);
    Task NackAsync(ConsumedTrackingMessage message, CancellationToken cancellationToken);
}

public sealed record ConsumedTrackingMessage(
    string Topic,
    int Partition,
    long Offset,
    CarrierTrackingEventIntegrationEvent Event);
