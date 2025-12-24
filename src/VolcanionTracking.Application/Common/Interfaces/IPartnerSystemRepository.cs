using VolcanionTracking.Domain.Aggregates.PartnerAggregate;

namespace VolcanionTracking.Application.Common.Interfaces;

/// <summary>
/// Repository interface for PartnerSystem entity
/// </summary>
public interface IPartnerSystemRepository
{
    Task<PartnerSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PartnerSystem?> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<List<PartnerSystem>> GetByPartnerIdAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task AddAsync(PartnerSystem system, CancellationToken cancellationToken = default);
    void Update(PartnerSystem system);
    void Delete(PartnerSystem system);
}
