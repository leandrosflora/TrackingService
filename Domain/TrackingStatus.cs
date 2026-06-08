namespace TrackingService.Domain;

public enum TrackingStatus
{
    Created = 1,
    LabelGenerated = 2,
    ReadyForPickup = 3,
    PickedUp = 4,
    InTransit = 5,
    AtDistributionCenter = 6,
    OutForDelivery = 7,
    DeliveryAttempted = 8,
    Delivered = 9,
    Exception = 10,
    Cancelled = 11,
    Returned = 12
}
