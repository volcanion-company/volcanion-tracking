using Microsoft.EntityFrameworkCore;
using VolcanionTracking.Application.Common.Interfaces;
using VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;
using VolcanionTracking.Infrastructure.Persistence;

namespace VolcanionTracking.Infrastructure.Persistence.Repositories;

public class TrackingEventReadRepository : ITrackingEventReadRepository
{
    private readonly ReadDbContext _context;

    public TrackingEventReadRepository(ReadDbContext context)
    {
        _context = context;
    }

    public async Task<TrackingEventReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TrackingEventReadModel>()
            .FirstOrDefaultAsync(te => te.Id == id, cancellationToken);
    }

    public async Task<List<TrackingEventReadModel>> GetByPartnerSystemIdAsync(
        Guid partnerSystemId,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TrackingEventReadModel>()
            .Where(te => te.PartnerSystemId == partnerSystemId);

        if (startDate.HasValue)
            query = query.Where(te => te.EventTimestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(te => te.EventTimestamp <= endDate.Value);

        return await query
            .OrderByDescending(te => te.EventTimestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TrackingEventReadModel>> GetByPartnerIdAsync(
        Guid partnerId,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TrackingEventReadModel>()
            .Where(te => te.PartnerId == partnerId);

        if (startDate.HasValue)
            query = query.Where(te => te.EventTimestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(te => te.EventTimestamp <= endDate.Value);

        return await query
            .OrderByDescending(te => te.EventTimestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<long> CountByPartnerSystemIdAsync(
        Guid partnerSystemId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TrackingEventReadModel>()
            .Where(te => te.PartnerSystemId == partnerSystemId);

        if (startDate.HasValue)
            query = query.Where(te => te.EventTimestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(te => te.EventTimestamp <= endDate.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<long> CountByEventNameAsync(
        Guid partnerSystemId,
        string eventName,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TrackingEventReadModel>()
            .Where(te => te.PartnerSystemId == partnerSystemId && te.EventName == eventName);

        if (startDate.HasValue)
            query = query.Where(te => te.EventTimestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(te => te.EventTimestamp <= endDate.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(TrackingEventReadModel readModel, CancellationToken cancellationToken = default)
    {
        await _context.Set<TrackingEventReadModel>().AddAsync(readModel, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
