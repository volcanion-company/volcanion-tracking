using VolcanionTracking.Domain.Aggregates.PartnerAggregate;

namespace VolcanionTracking.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Partner aggregate
/// </summary>
public interface IPartnerRepository
{
    Task<Partner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Partner?> GetByIdWithSystemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Partner>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Partner?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Partner partner, CancellationToken cancellationToken = default);
    void Update(Partner partner);
    void Delete(Partner partner);
}
