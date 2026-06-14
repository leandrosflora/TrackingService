namespace TrackingService.Contracts;

public sealed record ShipmentCreatedIntegrationEvent(
    Guid ShipmentId,
    string TrackingCode,
    string CarrierCode,
    DateOnly? EstimatedDeliveryDate,
    DateTimeOffset? CreatedAt);
