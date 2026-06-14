using TrackingService.Application.Ports;
using TrackingService.Contracts;

namespace TrackingService.Api;

public static class TrackingEndpoints
{
    public static IEndpointRouteBuilder MapTrackingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tracking").WithTags("Tracking");

        group.MapGet("/shipments/{shipmentId:guid}", async (
            Guid shipmentId,
            ITrackingRepository repository,
            CancellationToken cancellationToken) =>
        {
            var tracking = await repository.GetShipmentTrackingAsync(shipmentId, cancellationToken);

            return tracking is null ? Results.NotFound() : Results.Ok(MapTracking(tracking));
        });

        group.MapGet("/{trackingCode}", async (
            string trackingCode,
            ITrackingRepository repository,
            CancellationToken cancellationToken) =>
        {
            var tracking = await repository.GetShipmentTrackingByTrackingCodeAsync(trackingCode, cancellationToken);

            return tracking is null ? Results.NotFound() : Results.Ok(MapTracking(tracking));
        });

        group.MapGet("/shipments/{shipmentId:guid}/events", async (
            Guid shipmentId,
            int? limit,
            DateTimeOffset? before,
            ITrackingRepository repository,
            CancellationToken cancellationToken) =>
        {
            var events = await repository.GetTrackingEventsAsync(shipmentId, limit ?? 50, before, cancellationToken);

            return Results.Ok(events.Select(MapTrackingEvent));
        });

        return app;
    }

    private static ShipmentTrackingResponse MapTracking(Domain.ShipmentTracking tracking)
    {
        return new ShipmentTrackingResponse(
            tracking.ShipmentId,
            tracking.TrackingCode,
            tracking.CarrierCode,
            tracking.CurrentStatus,
            tracking.LastLocation is null
                ? null
                : new TrackingLocationDto(
                    tracking.LastLocation.FacilityCode,
                    tracking.LastLocation.City,
                    tracking.LastLocation.State,
                    tracking.LastLocation.Country),
            tracking.LastEventOccurredAt,
            tracking.EstimatedDeliveryDate,
            tracking.DeliveredAt,
            tracking.CurrentExceptionCode);
    }

    private static TrackingEventResponse MapTrackingEvent(Domain.TrackingEvent trackingEvent)
    {
        return new TrackingEventResponse(
            trackingEvent.Id,
            trackingEvent.Status,
            trackingEvent.Description,
            trackingEvent.ExceptionCode,
            trackingEvent.Location is null
                ? null
                : new TrackingLocationDto(
                    trackingEvent.Location.FacilityCode,
                    trackingEvent.Location.City,
                    trackingEvent.Location.State,
                    trackingEvent.Location.Country),
            trackingEvent.OccurredAt,
            trackingEvent.ReceivedAt);
    }
}
