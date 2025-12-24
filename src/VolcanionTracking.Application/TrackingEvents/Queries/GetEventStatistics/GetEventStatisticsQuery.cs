using MediatR;

namespace VolcanionTracking.Application.TrackingEvents.Queries.GetEventStatistics;

public record GetEventStatisticsQuery(
    Guid PartnerSystemId,
    DateTime? StartDate,
    DateTime? EndDate) : IRequest<EventStatisticsResult>;

public record EventStatisticsResult(
    Guid PartnerSystemId,
    long TotalEvents,
    long ValidEvents,
    long InvalidEvents,
    Dictionary<string, long> EventCounts,
    DateTime StartDate,
    DateTime EndDate);
