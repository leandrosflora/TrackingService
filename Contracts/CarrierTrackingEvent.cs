using TrackingService.Domain;

namespace TrackingService.Contracts;

public sealed record CarrierTrackingEventRequest(
    Guid? MessageId,
    string ProviderEventId,
    Guid ShipmentId,
    Guid OrderId,
    Guid BuyerId,
    string TrackingCode,
    string CarrierCode,
    long? CarrierSequence,
    TrackingStatus Status,
    string? Description,
    string? ExceptionCode,
    TrackingLocationDto? Location,
    DateTimeOffset OccurredAt,
    DateOnly? EstimatedDeliveryDate);

public sealed record TrackingEventAcceptedResponse(
    Guid MessageId,
    string CorrelationId,
    Guid ShipmentId,
    string ProviderEventId,
    string Status);

public sealed record CarrierTrackingEventIntegrationEvent(
    Guid MessageId,
    string CorrelationId,
    string ProviderEventId,
    Guid ShipmentId,
    Guid OrderId,
    Guid BuyerId,
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
