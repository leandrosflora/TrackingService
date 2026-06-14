using Microsoft.EntityFrameworkCore;
using TrackingService.Application.Ports;
using TrackingService.Domain;

namespace TrackingService.Infrastructure.Persistence;

public sealed class TrackingRepository : ITrackingRepository
{
    private readonly TrackingDbContext _dbContext;

    public TrackingRepository(TrackingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ShipmentTracking?> GetShipmentTrackingAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        return _dbContext.ShipmentTrackings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShipmentId == shipmentId, cancellationToken);
    }

    public Task<ShipmentTracking?> GetShipmentTrackingByTrackingCodeAsync(string trackingCode, CancellationToken cancellationToken)
    {
        return _dbContext.ShipmentTrackings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TrackingCode == trackingCode, cancellationToken);
    }

    public async Task<IReadOnlyList<TrackingEvent>> GetTrackingEventsAsync(
        Guid shipmentId,
        int limit,
        DateTimeOffset? before,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(limit, 1, 100);
        var query = _dbContext.TrackingEvents
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId);

        if (before.HasValue)
            query = query.Where(x => x.OccurredAt < before.Value);

        return await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.ReceivedAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
