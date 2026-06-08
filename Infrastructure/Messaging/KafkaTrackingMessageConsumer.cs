using System.Runtime.CompilerServices;

namespace TrackingService.Infrastructure.Messaging;

public sealed class KafkaTrackingMessageConsumer : ITrackingMessageConsumer
{
    private readonly ILogger<KafkaTrackingMessageConsumer> _logger;

    public KafkaTrackingMessageConsumer(ILogger<KafkaTrackingMessageConsumer> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<ConsumedTrackingMessage> ConsumeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka tracking consumer placeholder started");

        while (!cancellationToken.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

        yield break;
    }

    public Task CommitAsync(ConsumedTrackingMessage message, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Committed tracking message {Topic}/{Partition}/{Offset}", message.Topic, message.Partition, message.Offset);
        return Task.CompletedTask;
    }

    public Task NackAsync(ConsumedTrackingMessage message, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Nacked tracking message {Topic}/{Partition}/{Offset}", message.Topic, message.Partition, message.Offset);
        return Task.CompletedTask;
    }
}
