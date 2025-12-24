using VolcanionTracking.Domain.Common;

namespace VolcanionTracking.Domain.Aggregates.EventStructureAggregate;

// Domain Events
public record EventStructureCreatedEvent(Guid EventStructureId, string EventName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record EventStructureUpdatedEvent(Guid EventStructureId, string EventName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
