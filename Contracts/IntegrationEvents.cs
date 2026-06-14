using TrackingService.Domain;

namespace TrackingService.Contracts;

public sealed record TrackingStatusChangedIntegrationEvent(
    Guid MessageId,
    string CorrelationId,
    Guid ShipmentId,
    string TrackingCode,
    string CarrierCode,
    TrackingStatus PreviousStatus,
    TrackingStatus CurrentStatus,
    TrackingLocationDto? Location,
    DateTimeOffset OccurredAt,
    DateOnly? EstimatedDeliveryDate,
    string? ExceptionCode);
