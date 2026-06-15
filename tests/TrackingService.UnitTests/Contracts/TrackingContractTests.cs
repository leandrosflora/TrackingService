using System.Text.Json;
using TrackingService.Contracts;
using TrackingService.Domain;
using TrackingService.Infrastructure.Messaging;

namespace TrackingService.UnitTests.Contracts;

public sealed class TrackingContractTests
{
    [Fact]
    public void CarrierTrackingEventIntegrationEvent_PreservesRestKafkaContractFields()
    {
        var messageId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var location = new TrackingLocationDto("XD-01", "Sao Paulo", "SP", "BR");

        var integrationEvent = new CarrierTrackingEventIntegrationEvent(
            messageId,
            "corr-123",
            "carrier-event-123",
            shipmentId,
            orderId,
            buyerId,
            "MELI123",
            "MLB",
            25,
            TrackingStatus.OutForDelivery,
            "Out for delivery",
            null,
            location,
            DateTimeOffset.Parse("2026-06-14T10:15:00Z"),
            DateTimeOffset.Parse("2026-06-14T10:16:00Z"),
            DateOnly.Parse("2026-06-15"));

        Assert.Equal(messageId, integrationEvent.MessageId);
        Assert.Equal("corr-123", integrationEvent.CorrelationId);
        Assert.Equal("carrier-event-123", integrationEvent.ProviderEventId);
        Assert.Equal(shipmentId, integrationEvent.ShipmentId);
        Assert.Equal(orderId, integrationEvent.OrderId);
        Assert.Equal(buyerId, integrationEvent.BuyerId);
        Assert.Equal("MELI123", integrationEvent.TrackingCode);
        Assert.Equal("MLB", integrationEvent.CarrierCode);
        Assert.Equal(25, integrationEvent.CarrierSequence);
        Assert.Equal(TrackingStatus.OutForDelivery, integrationEvent.Status);
        Assert.Equal(location, integrationEvent.Location);
    }

    [Fact]
    public void IntegrationEventEnvelope_UsesStandardKafkaEnvelopeFields()
    {
        using var payloadDocument = JsonDocument.Parse("""
            { "shipmentId": "00000000-0000-0000-0000-000000000001", "currentStatus": "Delivered" }
            """);

        var envelope = new IntegrationEventEnvelope(
            Guid.NewGuid(),
            "shipment.status.updated",
            "1.0",
            DateTimeOffset.Parse("2026-06-14T10:15:00Z"),
            "corr-123",
            "TrackingService",
            payloadDocument.RootElement.Clone());

        Assert.NotEqual(Guid.Empty, envelope.EventId);
        Assert.Equal("shipment.status.updated", envelope.EventType);
        Assert.Equal("1.0", envelope.SchemaVersion);
        Assert.Equal("corr-123", envelope.CorrelationId);
        Assert.Equal("TrackingService", envelope.Producer);
        Assert.Equal("Delivered", envelope.Payload.GetProperty("currentStatus").GetString());
    }
}
