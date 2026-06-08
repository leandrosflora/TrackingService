using TrackingService.Application;

namespace TrackingService.Infrastructure.Messaging;

public sealed class TrackingConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITrackingMessageConsumer _consumer;
    private readonly ILogger<TrackingConsumerWorker> _logger;

    public TrackingConsumerWorker(
        IServiceScopeFactory scopeFactory,
        ITrackingMessageConsumer consumer,
        ILogger<TrackingConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _consumer.ConsumeAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<TrackingEventHandler>();
                await handler.HandleAsync(message.Event, stoppingToken);
                await _consumer.CommitAsync(message, stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Tracking event processing failed");
                await _consumer.NackAsync(message, stoppingToken);
            }
        }
    }
}
