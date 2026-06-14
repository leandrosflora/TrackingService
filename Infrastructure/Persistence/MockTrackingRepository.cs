using TrackingService.Application.Ports;
using TrackingService.Domain;

namespace TrackingService.Infrastructure.Persistence;

public sealed class MockTrackingRepository : ITrackingRepository
{
    private static readonly Guid ShipmentInTransitId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ShipmentDeliveredId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OrderInTransitId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid BuyerInTransitId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid OrderDeliveredId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid BuyerDeliveredId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static readonly IReadOnlyList<TrackingEvent> Events = BuildEvents();
    private static readonly IReadOnlyList<ShipmentTracking> Trackings = BuildTrackings();

    public Task<ShipmentTracking?> GetShipmentTrackingAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var tracking = Trackings.SingleOrDefault(x => x.ShipmentId == shipmentId);
        return Task.FromResult(tracking);
    }

    public Task<ShipmentTracking?> GetShipmentTrackingByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken)
    {
        var tracking = Trackings.SingleOrDefault(x => string.Equals(x.TrackingCode, trackingCode, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(tracking);
    }

    public Task<IReadOnlyList<TrackingEvent>> GetTrackingEventsAsync(
        Guid shipmentId,
        int limit,
        DateTimeOffset? before,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(limit, 1, 100);
        var query = Events
            .Where(x => x.ShipmentId == shipmentId);

        if (before.HasValue)
            query = query.Where(x => x.OccurredAt < before.Value);

        var events = query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.ReceivedAt)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<TrackingEvent>>(events);
    }

    private static IReadOnlyList<TrackingEvent> BuildEvents()
    {
        var receivedAt = DateTimeOffset.Parse("2026-06-14T12:00:00Z");

        return new[]
        {
            new TrackingEvent(
                ShipmentInTransitId,
                OrderInTransitId,
                BuyerInTransitId,
                "mock-mlb-001-created",
                "MLB-MOCK-IN-TRANSIT",
                "MELI",
                1,
                TrackingStatus.Created,
                "Envio criado e aguardando geração de etiqueta.",
                null,
                new TrackingLocation("FUL-SP01", "São Paulo", "SP", "BR"),
                DateTimeOffset.Parse("2026-06-12T09:00:00Z"),
                receivedAt,
                DateOnly.Parse("2026-06-18")),
            new TrackingEvent(
                ShipmentInTransitId,
                OrderInTransitId,
                BuyerInTransitId,
                "mock-mlb-001-picked-up",
                "MLB-MOCK-IN-TRANSIT",
                "MELI",
                2,
                TrackingStatus.PickedUp,
                "Pacote coletado pelo operador logístico.",
                null,
                new TrackingLocation("FUL-SP01", "São Paulo", "SP", "BR"),
                DateTimeOffset.Parse("2026-06-12T18:30:00Z"),
                receivedAt,
                DateOnly.Parse("2026-06-18")),
            new TrackingEvent(
                ShipmentInTransitId,
                OrderInTransitId,
                BuyerInTransitId,
                "mock-mlb-001-in-transit",
                "MLB-MOCK-IN-TRANSIT",
                "MELI",
                3,
                TrackingStatus.InTransit,
                "Pacote em transferência para o centro de distribuição.",
                null,
                new TrackingLocation("XD-RJ01", "Rio de Janeiro", "RJ", "BR"),
                DateTimeOffset.Parse("2026-06-13T10:15:00Z"),
                receivedAt,
                DateOnly.Parse("2026-06-18")),
            new TrackingEvent(
                ShipmentDeliveredId,
                OrderDeliveredId,
                BuyerDeliveredId,
                "mock-mlb-002-created",
                "MLB-MOCK-DELIVERED",
                "MELI",
                1,
                TrackingStatus.Created,
                "Envio criado.",
                null,
                new TrackingLocation("FUL-MG01", "Betim", "MG", "BR"),
                DateTimeOffset.Parse("2026-06-10T08:00:00Z"),
                receivedAt,
                DateOnly.Parse("2026-06-13")),
            new TrackingEvent(
                ShipmentDeliveredId,
                OrderDeliveredId,
                BuyerDeliveredId,
                "mock-mlb-002-delivered",
                "MLB-MOCK-DELIVERED",
                "MELI",
                2,
                TrackingStatus.Delivered,
                "Entrega realizada ao destinatário.",
                null,
                new TrackingLocation(null, "Belo Horizonte", "MG", "BR"),
                DateTimeOffset.Parse("2026-06-13T16:45:00Z"),
                receivedAt,
                DateOnly.Parse("2026-06-13"))
        };
    }

    private static IReadOnlyList<ShipmentTracking> BuildTrackings()
    {
        return Events
            .GroupBy(x => x.ShipmentId)
            .Select(group => ShipmentTracking.Create(group.OrderBy(x => x.OccurredAt).Last()))
            .ToList();
    }
}
