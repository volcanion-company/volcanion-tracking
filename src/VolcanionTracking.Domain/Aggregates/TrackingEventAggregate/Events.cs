using VolcanionTracking.Domain.Common;

namespace VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;

// Domain Events
public record TrackingEventReceivedEvent(
    Guid TrackingEventId,
    Guid PartnerSystemId,
    string EventName,
    bool IsValid) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
