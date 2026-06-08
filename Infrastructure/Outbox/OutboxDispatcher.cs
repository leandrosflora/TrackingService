using Microsoft.EntityFrameworkCore;
using TrackingService.Infrastructure.Messaging;
using TrackingService.Infrastructure.Persistence;

namespace TrackingService.Infrastructure.Outbox;

public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DispatchBatchAsync(stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
        var now = DateTimeOffset.UtcNow;

        var messages = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAt == null && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await eventBus.PublishAsync(
                    message.Topic,
                    message.AggregateKey,
                    message.MessageType,
                    message.Payload,
                    cancellationToken);

                message.MarkProcessed();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox message dispatch failed for {OutboxMessageId}", message.Id);
                message.MarkFailed();
            }
        }

        if (messages.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);
    }
}
