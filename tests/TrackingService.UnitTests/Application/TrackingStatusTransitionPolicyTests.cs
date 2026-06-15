using TrackingService.Application;
using TrackingService.Domain;

namespace TrackingService.UnitTests.Application;

public sealed class TrackingStatusTransitionPolicyTests
{
    private readonly TrackingStatusTransitionPolicy _policy = new();

    [Theory]
    [InlineData(TrackingStatus.Created, TrackingStatus.LabelGenerated)]
    [InlineData(TrackingStatus.LabelGenerated, TrackingStatus.PickedUp)]
    [InlineData(TrackingStatus.InTransit, TrackingStatus.Delivered)]
    [InlineData(TrackingStatus.Exception, TrackingStatus.OutForDelivery)]
    public void ShouldApply_AllowsDocumentedForwardTransitions(TrackingStatus currentStatus, TrackingStatus incomingStatus)
    {
        var current = ShipmentTracking.Create(CreateEvent(currentStatus, sequence: 10));
        var incoming = CreateEvent(incomingStatus, sequence: 11, shipmentId: current.ShipmentId);

        Assert.True(_policy.ShouldApply(current, incoming));
    }

    [Theory]
    [InlineData(TrackingStatus.Delivered)]
    [InlineData(TrackingStatus.Cancelled)]
    [InlineData(TrackingStatus.Returned)]
    public void ShouldApply_RejectsAnyIncomingEventAfterTerminalStatus(TrackingStatus terminalStatus)
    {
        var current = ShipmentTracking.Create(CreateEvent(terminalStatus, sequence: 10));
        var incoming = CreateEvent(TrackingStatus.InTransit, sequence: 11, shipmentId: current.ShipmentId);

        Assert.False(_policy.ShouldApply(current, incoming));
    }

    [Fact]
    public void ShouldApply_RejectsRepeatedOrOlderCarrierSequence()
    {
        var current = ShipmentTracking.Create(CreateEvent(TrackingStatus.InTransit, sequence: 10));
        var repeated = CreateEvent(TrackingStatus.OutForDelivery, sequence: 10, shipmentId: current.ShipmentId);
        var older = CreateEvent(TrackingStatus.OutForDelivery, sequence: 9, shipmentId: current.ShipmentId);

        Assert.False(_policy.ShouldApply(current, repeated));
        Assert.False(_policy.ShouldApply(current, older));
    }

    [Fact]
    public void ShouldApply_WhenSequenceIsMissingRejectsOlderOccurredAt()
    {
        var current = ShipmentTracking.Create(CreateEvent(TrackingStatus.InTransit, sequence: null));
        var older = CreateEvent(
            TrackingStatus.OutForDelivery,
            sequence: null,
            shipmentId: current.ShipmentId,
            occurredAt: current.LastEventOccurredAt.AddSeconds(-1));

        Assert.False(_policy.ShouldApply(current, older));
    }

    [Fact]
    public void ShouldApply_RejectsInvalidStatusRegression()
    {
        var current = ShipmentTracking.Create(CreateEvent(TrackingStatus.OutForDelivery, sequence: 10));
        var regression = CreateEvent(TrackingStatus.PickedUp, sequence: 11, shipmentId: current.ShipmentId);

        Assert.False(_policy.ShouldApply(current, regression));
    }

    private static TrackingEvent CreateEvent(
        TrackingStatus status,
        long? sequence,
        Guid? shipmentId = null,
        DateTimeOffset? occurredAt = null)
    {
        var eventTime = occurredAt ?? DateTimeOffset.Parse("2026-06-14T10:15:00Z").AddMinutes(sequence ?? 1);

        return new TrackingEvent(
            shipmentId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            $"provider-{status}-{sequence?.ToString() ?? eventTime.ToUnixTimeSeconds().ToString()}",
            "MELI123",
            "MLB",
            sequence,
            status,
            null,
            null,
            null,
            eventTime,
            eventTime.AddMinutes(1),
            null);
    }
}
