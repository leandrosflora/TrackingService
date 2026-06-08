using Microsoft.EntityFrameworkCore;
using TrackingService.Contracts;
using TrackingService.Infrastructure.Persistence;

namespace TrackingService.Api;

public static class TrackingEndpoints
{
    public static IEndpointRouteBuilder MapTrackingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tracking").WithTags("Tracking");

        group.MapGet("/shipments/{shipmentId:guid}", async (
            Guid shipmentId,
            TrackingDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var tracking = await dbContext.ShipmentTrackings
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.ShipmentId == shipmentId, cancellationToken);

            return tracking is null ? Results.NotFound() : Results.Ok(MapTracking(tracking));
        });

        group.MapGet("/{trackingCode}", async (
            string trackingCode,
            TrackingDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var tracking = await dbContext.ShipmentTrackings
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.TrackingCode == trackingCode, cancellationToken);

            return tracking is null ? Results.NotFound() : Results.Ok(MapTracking(tracking));
        });

        group.MapGet("/shipments/{shipmentId:guid}/events", async (
            Guid shipmentId,
            int? limit,
            DateTimeOffset? before,
            TrackingDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var pageSize = Math.Clamp(limit ?? 50, 1, 100);
            var query = dbContext.TrackingEvents
                .AsNoTracking()
                .Where(x => x.ShipmentId == shipmentId);

            if (before.HasValue)
                query = query.Where(x => x.OccurredAt < before.Value);

            var events = await query
                .OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.ReceivedAt)
                .Take(pageSize)
                .Select(x => new TrackingEventResponse(
                    x.Id,
                    x.Status,
                    x.Description,
                    x.ExceptionCode,
                    x.Location == null
                        ? null
                        : new TrackingLocationDto(
                            x.Location.FacilityCode,
                            x.Location.City,
                            x.Location.State,
                            x.Location.Country),
                    x.OccurredAt,
                    x.ReceivedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(events);
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
}
