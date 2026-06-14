namespace TrackingService.Contracts;

public sealed record ShipmentCreatedIntegrationEvent(
    Guid ShipmentId,
    Guid OrderId,
    Guid BuyerId,
    string TrackingCode,
    string CarrierCode,
    DateOnly? EstimatedDeliveryDate,
    DateTimeOffset CreatedAt);
