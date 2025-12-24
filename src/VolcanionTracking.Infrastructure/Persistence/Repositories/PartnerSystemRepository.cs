using Microsoft.EntityFrameworkCore;
using VolcanionTracking.Application.Common.Interfaces;
using VolcanionTracking.Domain.Aggregates.PartnerAggregate;
using VolcanionTracking.Infrastructure.Persistence;

namespace VolcanionTracking.Infrastructure.Persistence.Repositories;

public class PartnerSystemRepository : IPartnerSystemRepository
{
    private readonly WriteDbContext _context;

    public PartnerSystemRepository(WriteDbContext context)
    {
        _context = context;
    }

    public async Task<PartnerSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PartnerSystem>()
            .FirstOrDefaultAsync(ps => ps.Id == id, cancellationToken);
    }

    public async Task<PartnerSystem?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PartnerSystem>()
            .FirstOrDefaultAsync(ps => ps.ApiKey.Value == apiKey, cancellationToken);
    }

    public async Task<List<PartnerSystem>> GetByPartnerIdAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PartnerSystem>()
            .Where(ps => ps.PartnerId == partnerId)
            .OrderBy(ps => ps.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PartnerSystem system, CancellationToken cancellationToken = default)
    {
        await _context.Set<PartnerSystem>().AddAsync(system, cancellationToken);
    }

    public void Update(PartnerSystem system)
    {
        _context.Set<PartnerSystem>().Update(system);
    }

    public void Delete(PartnerSystem system)
    {
        _context.Set<PartnerSystem>().Remove(system);
    }
}
