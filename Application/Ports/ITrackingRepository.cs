using TrackingService.Domain;

namespace TrackingService.Application.Ports;

public interface ITrackingRepository
{
    Task<ShipmentTracking?> GetShipmentTrackingAsync(Guid shipmentId, CancellationToken cancellationToken);
    Task<ShipmentTracking?> GetShipmentTrackingByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<TrackingEvent>> GetTrackingEventsAsync(
        Guid shipmentId,
        int limit,
        DateTimeOffset? before,
        CancellationToken cancellationToken);
}
