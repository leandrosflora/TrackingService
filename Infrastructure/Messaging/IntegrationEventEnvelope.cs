using System.Text.Json;

namespace TrackingService.Infrastructure.Messaging;

public sealed record IntegrationEventEnvelope(
    Guid EventId,
    string EventType,
    string SchemaVersion,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string Producer,
    JsonElement Payload);
