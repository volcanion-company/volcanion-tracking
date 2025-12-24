using Microsoft.EntityFrameworkCore;
using VolcanionTracking.Application.Common.Interfaces;
using VolcanionTracking.Domain.Aggregates.PartnerAggregate;
using VolcanionTracking.Infrastructure.Persistence;

namespace VolcanionTracking.Infrastructure.Persistence.Repositories;

public class PartnerRepository : IPartnerRepository
{
    private readonly WriteDbContext _context;

    public PartnerRepository(WriteDbContext context)
    {
        _context = context;
    }

    public async Task<Partner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Partner>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Partner?> GetByIdWithSystemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Partner>()
            .Include(p => p.Systems)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<Partner>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Partner>()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Partner?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Partner>()
            .FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
    }

    public async Task AddAsync(Partner partner, CancellationToken cancellationToken = default)
    {
        await _context.Set<Partner>().AddAsync(partner, cancellationToken);
    }

    public void Update(Partner partner)
    {
        _context.Set<Partner>().Update(partner);
    }

    public void Delete(Partner partner)
    {
        _context.Set<Partner>().Remove(partner);
    }
}
