using System.Text.Json;
using System.Text.Json.Serialization;
using TrackingService.Application.Ports;
using TrackingService.Infrastructure.Persistence;

namespace TrackingService.Infrastructure.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private readonly TrackingDbContext _dbContext;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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
            JsonSerializer.Serialize(message, JsonOptions));

        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
