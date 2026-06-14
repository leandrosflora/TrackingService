using Microsoft.EntityFrameworkCore;
using TrackingService.Application.Ports;
using TrackingService.Contracts;
using TrackingService.Domain;
using TrackingService.Infrastructure.Persistence;

namespace TrackingService.Application;

public sealed class TrackingEventHandler
{
    private readonly TrackingDbContext _dbContext;
    private readonly TrackingStatusTransitionPolicy _transitionPolicy;
    private readonly IOutboxWriter _outbox;

    public TrackingEventHandler(
        TrackingDbContext dbContext,
        TrackingStatusTransitionPolicy transitionPolicy,
        IOutboxWriter outbox)
    {
        _dbContext = dbContext;
        _transitionPolicy = transitionPolicy;
        _outbox = outbox;
    }

    public async Task HandleAsync(CarrierTrackingEventIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(x => x.MessageId == integrationEvent.MessageId, cancellationToken))
            return;

        var normalizedCarrierCode = integrationEvent.CarrierCode.Trim().ToUpperInvariant();
        var providerEventExists = await _dbContext.TrackingEvents.AnyAsync(
            x => x.CarrierCode == normalizedCarrierCode && x.ProviderEventId == integrationEvent.ProviderEventId,
            cancellationToken);

        if (providerEventExists)
        {
            await RegisterInboxOnlyAsync(integrationEvent, cancellationToken);
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var trackingEvent = MapEvent(integrationEvent);
        await _dbContext.TrackingEvents.AddAsync(trackingEvent, cancellationToken);

        var current = await _dbContext.ShipmentTrackings
            .FromSqlInterpolated($"""
                SELECT *
                FROM shipment_tracking
                WHERE shipment_id = {integrationEvent.ShipmentId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        TrackingStatus? previousStatus = null;
        var projectionChanged = false;

        if (current is null)
        {
            current = ShipmentTracking.Create(trackingEvent);
            await _dbContext.ShipmentTrackings.AddAsync(current, cancellationToken);
            projectionChanged = true;
        }
        else if (_transitionPolicy.ShouldApply(current, trackingEvent))
        {
            previousStatus = current.CurrentStatus;
            current.Apply(trackingEvent);
            projectionChanged = true;
        }

        if (projectionChanged)
        {
            await _outbox.AddAsync(
                topic: "shipment.status.updated",
                aggregateKey: integrationEvent.ShipmentId.ToString(),
                message: new TrackingStatusChangedIntegrationEvent(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: integrationEvent.CorrelationId,
                    ShipmentId: integrationEvent.ShipmentId,
                    OrderId: integrationEvent.OrderId,
                    BuyerId: integrationEvent.BuyerId,
                    TrackingCode: trackingEvent.TrackingCode,
                    CarrierCode: trackingEvent.CarrierCode,
                    PreviousStatus: previousStatus ?? TrackingStatus.Created,
                    CurrentStatus: trackingEvent.Status,
                    Location: integrationEvent.Location,
                    StatusDate: trackingEvent.OccurredAt,
                    EstimatedDeliveryDate: trackingEvent.EstimatedDeliveryDate,
                    ExceptionCode: trackingEvent.ExceptionCode),
                cancellationToken);
        }

        await _dbContext.InboxMessages.AddAsync(
            new InboxMessage(integrationEvent.MessageId, nameof(CarrierTrackingEventIntegrationEvent)),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RegisterInboxOnlyAsync(
        CarrierTrackingEventIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await _dbContext.InboxMessages.AddAsync(
            new InboxMessage(integrationEvent.MessageId, nameof(CarrierTrackingEventIntegrationEvent)),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TrackingEvent MapEvent(CarrierTrackingEventIntegrationEvent source)
    {
        var location = source.Location is null
            ? null
            : new TrackingLocation(
                source.Location.FacilityCode,
                source.Location.City,
                source.Location.State,
                source.Location.Country);

        return new TrackingEvent(
            source.ShipmentId,
            source.OrderId,
            source.BuyerId,
            source.ProviderEventId,
            source.TrackingCode,
            source.CarrierCode,
            source.CarrierSequence,
            source.Status,
            source.Description,
            source.ExceptionCode,
            location,
            source.OccurredAt,
            source.ReceivedAt,
            source.EstimatedDeliveryDate);
    }
}
