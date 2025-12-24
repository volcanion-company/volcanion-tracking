using VolcanionTracking.Domain.Common;

namespace VolcanionTracking.Domain.Aggregates.PartnerAggregate;

// Domain Events
public record PartnerCreatedEvent(Guid PartnerId, string PartnerName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PartnerDeactivatedEvent(Guid PartnerId, string PartnerName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PartnerActivatedEvent(Guid PartnerId, string PartnerName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record PartnerSystemAddedEvent(Guid PartnerId, Guid SystemId, string SystemName, SystemType Type) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
