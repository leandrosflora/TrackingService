using TrackingService.Domain;

namespace TrackingService.Application;

public sealed class TrackingStatusTransitionPolicy
{
    public bool ShouldApply(ShipmentTracking current, TrackingEvent incoming)
    {
        if (IsTerminal(current.CurrentStatus))
            return false;

        if (incoming.CarrierSequence.HasValue && current.LastCarrierSequence.HasValue)
        {
            if (incoming.CarrierSequence.Value <= current.LastCarrierSequence.Value)
                return false;
        }
        else if (incoming.OccurredAt < current.LastEventOccurredAt)
        {
            return false;
        }

        return IsAllowedTransition(current.CurrentStatus, incoming.Status);
    }

    private static bool IsTerminal(TrackingStatus status)
    {
        return status is TrackingStatus.Delivered or TrackingStatus.Cancelled or TrackingStatus.Returned;
    }

    private static bool IsAllowedTransition(TrackingStatus current, TrackingStatus incoming)
    {
        if (current == incoming)
            return true;

        return current switch
        {
            TrackingStatus.Created => incoming is
                TrackingStatus.LabelGenerated or
                TrackingStatus.ReadyForPickup or
                TrackingStatus.PickedUp or
                TrackingStatus.Cancelled,
            TrackingStatus.LabelGenerated => incoming is
                TrackingStatus.ReadyForPickup or
                TrackingStatus.PickedUp or
                TrackingStatus.Cancelled,
            TrackingStatus.ReadyForPickup => incoming is
                TrackingStatus.PickedUp or
                TrackingStatus.Cancelled or
                TrackingStatus.Exception,
            TrackingStatus.PickedUp => incoming is
                TrackingStatus.InTransit or
                TrackingStatus.AtDistributionCenter or
                TrackingStatus.Exception,
            TrackingStatus.InTransit => incoming is
                TrackingStatus.AtDistributionCenter or
                TrackingStatus.OutForDelivery or
                TrackingStatus.DeliveryAttempted or
                TrackingStatus.Delivered or
                TrackingStatus.Exception or
                TrackingStatus.Returned,
            TrackingStatus.AtDistributionCenter => incoming is
                TrackingStatus.InTransit or
                TrackingStatus.OutForDelivery or
                TrackingStatus.Exception or
                TrackingStatus.Returned,
            TrackingStatus.OutForDelivery => incoming is
                TrackingStatus.Delivered or
                TrackingStatus.DeliveryAttempted or
                TrackingStatus.Exception,
            TrackingStatus.DeliveryAttempted => incoming is
                TrackingStatus.OutForDelivery or
                TrackingStatus.InTransit or
                TrackingStatus.Delivered or
                TrackingStatus.Exception or
                TrackingStatus.Returned,
            TrackingStatus.Exception => incoming is
                TrackingStatus.InTransit or
                TrackingStatus.AtDistributionCenter or
                TrackingStatus.OutForDelivery or
                TrackingStatus.DeliveryAttempted or
                TrackingStatus.Delivered or
                TrackingStatus.Cancelled or
                TrackingStatus.Returned,
            _ => false
        };
    }
}
