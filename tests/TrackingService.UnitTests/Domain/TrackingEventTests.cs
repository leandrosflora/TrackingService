using TrackingService.Domain;

namespace TrackingService.UnitTests.Domain;

public sealed class TrackingEventTests
{
    [Fact]
    public void Constructor_NormalizesCarrierCodeAndKeepsContractFields()
    {
        var shipmentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.Parse("2026-06-14T10:15:00Z");
        var receivedAt = occurredAt.AddMinutes(2);
        var estimatedDeliveryDate = DateOnly.FromDateTime(occurredAt.DateTime.AddDays(2));
        var location = new TrackingLocation("XD-01", "Sao Paulo", "SP", "BR");

        var trackingEvent = new TrackingEvent(
            shipmentId,
            orderId,
            buyerId,
            "provider-event-1",
            "MELI123",
            " mlb ",
            42,
            TrackingStatus.InTransit,
            "Shipment departed distribution center",
            exceptionCode: null,
            location,
            occurredAt,
            receivedAt,
            estimatedDeliveryDate);

        Assert.NotEqual(Guid.Empty, trackingEvent.Id);
        Assert.Equal(shipmentId, trackingEvent.ShipmentId);
        Assert.Equal(orderId, trackingEvent.OrderId);
        Assert.Equal(buyerId, trackingEvent.BuyerId);
        Assert.Equal("provider-event-1", trackingEvent.ProviderEventId);
        Assert.Equal("MELI123", trackingEvent.TrackingCode);
        Assert.Equal("MLB", trackingEvent.CarrierCode);
        Assert.Equal(42, trackingEvent.CarrierSequence);
        Assert.Equal(TrackingStatus.InTransit, trackingEvent.Status);
        Assert.Same(location, trackingEvent.Location);
        Assert.Equal(occurredAt, trackingEvent.OccurredAt);
        Assert.Equal(receivedAt, trackingEvent.ReceivedAt);
        Assert.Equal(estimatedDeliveryDate, trackingEvent.EstimatedDeliveryDate);
    }

    [Theory]
    [InlineData("providerEventId")]
    [InlineData("trackingCode")]
    [InlineData("carrierCode")]
    public void Constructor_RejectsRequiredTextFields(string invalidField)
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateEvent(invalidField));

        Assert.Contains("is required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_RejectsRequiredIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => CreateEvent(shipmentId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => CreateEvent(orderId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => CreateEvent(buyerId: Guid.Empty));
    }

    private static TrackingEvent CreateEvent(
        string? invalidField = null,
        Guid? shipmentId = null,
        Guid? orderId = null,
        Guid? buyerId = null)
    {
        return new TrackingEvent(
            shipmentId ?? Guid.NewGuid(),
            orderId ?? Guid.NewGuid(),
            buyerId ?? Guid.NewGuid(),
            invalidField == "providerEventId" ? " " : "provider-event-1",
            invalidField == "trackingCode" ? " " : "MELI123",
            invalidField == "carrierCode" ? " " : "MLB",
            1,
            TrackingStatus.Created,
            null,
            null,
            null,
            DateTimeOffset.Parse("2026-06-14T10:15:00Z"),
            DateTimeOffset.Parse("2026-06-14T10:16:00Z"),
            null);
    }
}
