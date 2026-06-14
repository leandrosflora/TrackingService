namespace TrackingService.Domain;

public sealed class TrackingEvent
{
    public Guid Id { get; private set; }
    public Guid ShipmentId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid BuyerId { get; private set; }
    public string ProviderEventId { get; private set; } = default!;
    public string TrackingCode { get; private set; } = default!;
    public string CarrierCode { get; private set; } = default!;
    public long? CarrierSequence { get; private set; }
    public TrackingStatus Status { get; private set; }
    public string? Description { get; private set; }
    public string? ExceptionCode { get; private set; }
    public TrackingLocation? Location { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateOnly? EstimatedDeliveryDate { get; private set; }

    private TrackingEvent()
    {
    }

    public TrackingEvent(
        Guid shipmentId,
        Guid orderId,
        Guid buyerId,
        string providerEventId,
        string trackingCode,
        string carrierCode,
        long? carrierSequence,
        TrackingStatus status,
        string? description,
        string? exceptionCode,
        TrackingLocation? location,
        DateTimeOffset occurredAt,
        DateTimeOffset receivedAt,
        DateOnly? estimatedDeliveryDate)
    {
        if (shipmentId == Guid.Empty)
            throw new ArgumentException("ShipmentId is required", nameof(shipmentId));

        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId is required", nameof(orderId));

        if (buyerId == Guid.Empty)
            throw new ArgumentException("BuyerId is required", nameof(buyerId));

        if (string.IsNullOrWhiteSpace(providerEventId))
            throw new ArgumentException("ProviderEventId is required", nameof(providerEventId));

        if (string.IsNullOrWhiteSpace(trackingCode))
            throw new ArgumentException("TrackingCode is required", nameof(trackingCode));

        if (string.IsNullOrWhiteSpace(carrierCode))
            throw new ArgumentException("CarrierCode is required", nameof(carrierCode));

        Id = Guid.NewGuid();
        ShipmentId = shipmentId;
        OrderId = orderId;
        BuyerId = buyerId;
        ProviderEventId = providerEventId;
        TrackingCode = trackingCode;
        CarrierCode = carrierCode.Trim().ToUpperInvariant();
        CarrierSequence = carrierSequence;
        Status = status;
        Description = description;
        ExceptionCode = exceptionCode;
        Location = location;
        OccurredAt = occurredAt;
        ReceivedAt = receivedAt;
        EstimatedDeliveryDate = estimatedDeliveryDate;
    }
}
