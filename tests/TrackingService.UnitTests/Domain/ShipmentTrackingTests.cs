using TrackingService.Domain;

namespace TrackingService.UnitTests.Domain;

public sealed class ShipmentTrackingTests
{
    [Fact]
    public void Create_ProjectsInitialTrackingEvent()
    {
        var trackingEvent = CreateTrackingEvent(TrackingStatus.PickedUp, sequence: 10);

        var tracking = ShipmentTracking.Create(trackingEvent);

        Assert.Equal(trackingEvent.ShipmentId, tracking.ShipmentId);
        Assert.Equal(trackingEvent.OrderId, tracking.OrderId);
        Assert.Equal(trackingEvent.BuyerId, tracking.BuyerId);
        Assert.Equal(trackingEvent.TrackingCode, tracking.TrackingCode);
        Assert.Equal(trackingEvent.CarrierCode, tracking.CarrierCode);
        Assert.Equal(TrackingStatus.PickedUp, tracking.CurrentStatus);
        Assert.Equal(trackingEvent.Id, tracking.LastEventId);
        Assert.Equal(10, tracking.LastCarrierSequence);
        Assert.Null(tracking.DeliveredAt);
        Assert.Null(tracking.CurrentExceptionCode);
        Assert.Equal(1, tracking.Version);
    }

    [Fact]
    public void Apply_DeliveredEventUpdatesStatusAndDeliveredAt()
    {
        var initial = CreateTrackingEvent(TrackingStatus.InTransit, sequence: 20);
        var tracking = ShipmentTracking.Create(initial);
        var delivered = CreateTrackingEvent(
            TrackingStatus.Delivered,
            sequence: 21,
            shipmentId: initial.ShipmentId,
            orderId: initial.OrderId,
            buyerId: initial.BuyerId,
            occurredAt: initial.OccurredAt.AddHours(4));

        tracking.Apply(delivered);

        Assert.Equal(TrackingStatus.Delivered, tracking.CurrentStatus);
        Assert.Equal(delivered.Id, tracking.LastEventId);
        Assert.Equal(delivered.OccurredAt, tracking.DeliveredAt);
        Assert.Equal(2, tracking.Version);
    }

    [Fact]
    public void Apply_ExceptionEventStoresExceptionCodeAndRecoveryClearsIt()
    {
        var initial = CreateTrackingEvent(TrackingStatus.InTransit, sequence: 20);
        var tracking = ShipmentTracking.Create(initial);
        var exception = CreateTrackingEvent(
            TrackingStatus.Exception,
            sequence: 21,
            shipmentId: initial.ShipmentId,
            orderId: initial.OrderId,
            buyerId: initial.BuyerId,
            exceptionCode: "ADDRESS_NOT_FOUND");
        var recovered = CreateTrackingEvent(
            TrackingStatus.OutForDelivery,
            sequence: 22,
            shipmentId: initial.ShipmentId,
            orderId: initial.OrderId,
            buyerId: initial.BuyerId);

        tracking.Apply(exception);
        Assert.Equal("ADDRESS_NOT_FOUND", tracking.CurrentExceptionCode);

        tracking.Apply(recovered);
        Assert.Null(tracking.CurrentExceptionCode);
    }

    private static TrackingEvent CreateTrackingEvent(
        TrackingStatus status,
        long sequence,
        Guid? shipmentId = null,
        Guid? orderId = null,
        Guid? buyerId = null,
        DateTimeOffset? occurredAt = null,
        string? exceptionCode = null)
    {
        return new TrackingEvent(
            shipmentId ?? Guid.NewGuid(),
            orderId ?? Guid.NewGuid(),
            buyerId ?? Guid.NewGuid(),
            $"provider-{sequence}",
            "MELI123",
            "MLB",
            sequence,
            status,
            null,
            exceptionCode,
            new TrackingLocation("XD-01", "Sao Paulo", "SP", "BR"),
            occurredAt ?? DateTimeOffset.Parse("2026-06-14T10:15:00Z").AddMinutes(sequence),
            (occurredAt ?? DateTimeOffset.Parse("2026-06-14T10:15:00Z").AddMinutes(sequence)).AddMinutes(1),
            DateOnly.Parse("2026-06-20"));
    }
}
