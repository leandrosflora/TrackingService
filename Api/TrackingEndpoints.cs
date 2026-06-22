using TrackingService.Application;
using TrackingService.Application.Ports;
using TrackingService.Contracts;

namespace TrackingService.Api;

public static class TrackingEndpoints
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    public static IEndpointRouteBuilder MapTrackingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/tracking").WithTags("TrackingService");

        group.MapGet("/{trackingCode}", async (
            string trackingCode,
            ITrackingRepository repository,
            CancellationToken cancellationToken) =>
        {
            var tracking = await repository.GetShipmentTrackingByTrackingCodeAsync(trackingCode, cancellationToken);
            if (tracking is null)
                return Results.NotFound();

            var events = await repository.GetTrackingEventsAsync(tracking.ShipmentId, 100, null, cancellationToken);

            return Results.Ok(MapTracking(tracking, events));
        });

        group.MapGet("/shipments/{shipmentId:guid}", async (
            Guid shipmentId,
            ITrackingRepository repository,
            CancellationToken cancellationToken) =>
        {
            var tracking = await repository.GetShipmentTrackingAsync(shipmentId, cancellationToken);
            if (tracking is null)
                return Results.NotFound();

            var events = await repository.GetTrackingEventsAsync(shipmentId, 100, null, cancellationToken);

            return Results.Ok(MapTracking(tracking, events));
        });

        group.MapPost("/events", async (
            CarrierTrackingEventRequest request,
            HttpContext httpContext,
            TrackingEventHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationError = ValidateCarrierEventRequest(request);
            if (validationError is not null)
                return Results.BadRequest(new { message = validationError });

            var integrationEvent = MapCarrierTrackingEvent(request, ResolveCorrelationId(httpContext));
            await handler.HandleAsync(integrationEvent, cancellationToken);

            return Results.Accepted($"/v1/tracking/shipments/{request.ShipmentId}", new TrackingEventAcceptedResponse(
                integrationEvent.MessageId,
                integrationEvent.CorrelationId,
                integrationEvent.ShipmentId,
                integrationEvent.ProviderEventId,
                "accepted"));
        });

        return app;
    }

    private static ShipmentTrackingResponse MapTracking(
        Domain.ShipmentTracking tracking,
        IReadOnlyList<Domain.TrackingEvent> events)
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
            tracking.CurrentExceptionCode,
            events.OrderBy(x => x.OccurredAt).Select(MapTrackingEvent).ToArray());
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

    private static CarrierTrackingEventIntegrationEvent MapCarrierTrackingEvent(
        CarrierTrackingEventRequest request,
        string correlationId)
    {
        return new CarrierTrackingEventIntegrationEvent(
            MessageId: request.MessageId ?? Guid.NewGuid(),
            CorrelationId: correlationId,
            ProviderEventId: request.ProviderEventId.Trim(),
            ShipmentId: request.ShipmentId,
            OrderId: request.OrderId,
            BuyerId: request.BuyerId,
            TrackingCode: request.TrackingCode.Trim(),
            CarrierCode: request.CarrierCode.Trim(),
            CarrierSequence: request.CarrierSequence,
            Status: request.Status,
            Description: request.Description,
            ExceptionCode: request.ExceptionCode,
            Location: request.Location,
            OccurredAt: request.OccurredAt,
            ReceivedAt: DateTimeOffset.UtcNow,
            EstimatedDeliveryDate: request.EstimatedDeliveryDate);
    }

    private static string ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
            return headerValue.ToString();

        return httpContext.TraceIdentifier;
    }

    private static string? ValidateCarrierEventRequest(CarrierTrackingEventRequest request)
    {
        if (request.ShipmentId == Guid.Empty)
            return "shipmentId is required.";
        if (request.OrderId == Guid.Empty)
            return "orderId is required.";
        if (request.BuyerId == Guid.Empty)
            return "buyerId is required.";
        if (string.IsNullOrWhiteSpace(request.ProviderEventId))
            return "providerEventId is required.";
        if (string.IsNullOrWhiteSpace(request.TrackingCode))
            return "trackingCode is required.";
        if (string.IsNullOrWhiteSpace(request.CarrierCode))
            return "carrierCode is required.";
        if (request.OccurredAt == default)
            return "occurredAt is required.";

        return null;
    }
}
