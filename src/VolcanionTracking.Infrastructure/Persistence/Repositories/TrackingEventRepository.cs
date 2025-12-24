using Microsoft.EntityFrameworkCore;
using VolcanionTracking.Application.Common.Interfaces;
using VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;
using VolcanionTracking.Infrastructure.Persistence;

namespace VolcanionTracking.Infrastructure.Persistence.Repositories;

public class TrackingEventRepository : ITrackingEventRepository
{
    private readonly WriteDbContext _context;

    public TrackingEventRepository(WriteDbContext context)
    {
        _context = context;
    }

    public async Task<TrackingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TrackingEvent>()
            .FirstOrDefaultAsync(te => te.Id == id, cancellationToken);
    }

    public async Task AddAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken = default)
    {
        await _context.Set<TrackingEvent>().AddAsync(trackingEvent, cancellationToken);
    }

    public async Task AddBatchAsync(List<TrackingEvent> trackingEvents, CancellationToken cancellationToken = default)
    {
        await _context.Set<TrackingEvent>().AddRangeAsync(trackingEvents, cancellationToken);
    }
}
