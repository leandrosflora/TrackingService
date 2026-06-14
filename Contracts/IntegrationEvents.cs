using TrackingService.Domain;

namespace TrackingService.Contracts;

public sealed record TrackingStatusChangedIntegrationEvent(
    Guid MessageId,
    string CorrelationId,
    Guid ShipmentId,
    Guid OrderId,
    Guid BuyerId,
    string TrackingCode,
    string CarrierCode,
    TrackingStatus PreviousStatus,
    TrackingStatus CurrentStatus,
    TrackingLocationDto? Location,
    DateTimeOffset StatusDate,
    DateOnly? EstimatedDeliveryDate,
    string? ExceptionCode);
