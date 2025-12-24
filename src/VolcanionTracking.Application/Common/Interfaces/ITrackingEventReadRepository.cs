using VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;

namespace VolcanionTracking.Application.Common.Interfaces;

/// <summary>
/// Repository interface for TrackingEventReadModel (Read model)
/// </summary>
public interface ITrackingEventReadRepository
{
    Task<TrackingEventReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TrackingEventReadModel>> GetByPartnerSystemIdAsync(
        Guid partnerSystemId, 
        DateTime? startDate, 
        DateTime? endDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<List<TrackingEventReadModel>> GetByPartnerIdAsync(
        Guid partnerId,
        DateTime? startDate,
        DateTime? endDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<long> CountByPartnerSystemIdAsync(
        Guid partnerSystemId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default);
    Task<long> CountByEventNameAsync(
        Guid partnerSystemId,
        string eventName,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default);
    Task AddAsync(TrackingEventReadModel readModel, CancellationToken cancellationToken = default);
}
