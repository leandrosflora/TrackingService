using TrackingService.Domain;

namespace TrackingService.Contracts;

public sealed record CarrierTrackingEventIntegrationEvent(
    Guid MessageId,
    string ProviderEventId,
    Guid ShipmentId,
    string TrackingCode,
    string CarrierCode,
    long? CarrierSequence,
    TrackingStatus Status,
    string? Description,
    string? ExceptionCode,
    TrackingLocationDto? Location,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt,
    DateOnly? EstimatedDeliveryDate);

public sealed record TrackingLocationDto(
    string? FacilityCode,
    string? City,
    string? State,
    string? Country);
