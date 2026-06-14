namespace TrackingService.Domain;

public sealed class ShipmentTracking
{
    public Guid ShipmentId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid BuyerId { get; private set; }
    public string TrackingCode { get; private set; } = default!;
    public string CarrierCode { get; private set; } = default!;
    public TrackingStatus CurrentStatus { get; private set; }
    public Guid LastEventId { get; private set; }
    public long? LastCarrierSequence { get; private set; }
    public DateTimeOffset LastEventOccurredAt { get; private set; }
    public DateTimeOffset LastEventReceivedAt { get; private set; }
    public TrackingLocation? LastLocation { get; private set; }
    public DateOnly? EstimatedDeliveryDate { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public string? CurrentExceptionCode { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ShipmentTracking()
    {
    }

    public static ShipmentTracking Create(TrackingEvent trackingEvent)
    {
        var now = DateTimeOffset.UtcNow;

        return new ShipmentTracking
        {
            ShipmentId = trackingEvent.ShipmentId,
            OrderId = trackingEvent.OrderId,
            BuyerId = trackingEvent.BuyerId,
            TrackingCode = trackingEvent.TrackingCode,
            CarrierCode = trackingEvent.CarrierCode,
            CurrentStatus = trackingEvent.Status,
            LastEventId = trackingEvent.Id,
            LastCarrierSequence = trackingEvent.CarrierSequence,
            LastEventOccurredAt = trackingEvent.OccurredAt,
            LastEventReceivedAt = trackingEvent.ReceivedAt,
            LastLocation = trackingEvent.Location,
            EstimatedDeliveryDate = trackingEvent.EstimatedDeliveryDate,
            DeliveredAt = trackingEvent.Status == TrackingStatus.Delivered ? trackingEvent.OccurredAt : null,
            CurrentExceptionCode = trackingEvent.Status == TrackingStatus.Exception ? trackingEvent.ExceptionCode : null,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Apply(TrackingEvent trackingEvent)
    {
        OrderId = trackingEvent.OrderId;
        BuyerId = trackingEvent.BuyerId;
        CurrentStatus = trackingEvent.Status;
        LastEventId = trackingEvent.Id;
        LastCarrierSequence = trackingEvent.CarrierSequence;
        LastEventOccurredAt = trackingEvent.OccurredAt;
        LastEventReceivedAt = trackingEvent.ReceivedAt;
        LastLocation = trackingEvent.Location ?? LastLocation;
        EstimatedDeliveryDate = trackingEvent.EstimatedDeliveryDate ?? EstimatedDeliveryDate;
        CurrentExceptionCode = trackingEvent.Status == TrackingStatus.Exception ? trackingEvent.ExceptionCode : null;

        if (trackingEvent.Status == TrackingStatus.Delivered)
            DeliveredAt = trackingEvent.OccurredAt;

        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
