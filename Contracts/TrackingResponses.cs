using TrackingService.Domain;

namespace TrackingService.Contracts;

public sealed record ShipmentTrackingResponse(
    Guid ShipmentId,
    string TrackingCode,
    string CarrierCode,
    TrackingStatus CurrentStatus,
    TrackingLocationDto? LastLocation,
    DateTimeOffset LastEventOccurredAt,
    DateOnly? EstimatedDeliveryDate,
    DateTimeOffset? DeliveredAt,
    string? CurrentExceptionCode);

public sealed record TrackingEventResponse(
    Guid EventId,
    TrackingStatus Status,
    string? Description,
    string? ExceptionCode,
    TrackingLocationDto? Location,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt);
