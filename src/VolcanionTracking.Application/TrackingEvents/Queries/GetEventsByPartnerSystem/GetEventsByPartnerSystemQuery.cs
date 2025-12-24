using MediatR;

namespace VolcanionTracking.Application.TrackingEvents.Queries.GetEventsByPartnerSystem;

public record GetEventsByPartnerSystemQuery(
    Guid PartnerSystemId,
    DateTime? StartDate,
    DateTime? EndDate,
    int PageNumber = 1,
    int PageSize = 100) : IRequest<GetEventsByPartnerSystemResult>;

public record GetEventsByPartnerSystemResult(
    List<TrackingEventDto> Events,
    long TotalCount,
    int PageNumber,
    int PageSize);

public record TrackingEventDto(
    Guid Id,
    Guid PartnerSystemId,
    Guid PartnerId,
    string PartnerName,
    string SystemName,
    string EventName,
    DateTime EventTimestamp,
    string? UserId,
    string AnonymousId,
    string EventPropertiesJson,
    bool IsValid,
    string? ValidationErrors,
    string CorrelationId);
