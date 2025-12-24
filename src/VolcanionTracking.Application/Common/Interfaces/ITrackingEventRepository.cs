using VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;

namespace VolcanionTracking.Application.Common.Interfaces;

/// <summary>
/// Repository interface for TrackingEvent aggregate (Write model)
/// </summary>
public interface ITrackingEventRepository
{
    Task<TrackingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken = default);
    Task AddBatchAsync(List<TrackingEvent> trackingEvents, CancellationToken cancellationToken = default);
}
