using System.Text.Json;
using TrackingService.Application.Ports;
using TrackingService.Infrastructure.Persistence;

namespace TrackingService.Infrastructure.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly TrackingDbContext _dbContext;

    public OutboxWriter(TrackingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync<T>(string topic, string aggregateKey, T message, CancellationToken cancellationToken)
    {
        var outboxMessage = new OutboxMessage(
            topic,
            typeof(T).Name,
            aggregateKey,
            JsonSerializer.Serialize(message));

        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
