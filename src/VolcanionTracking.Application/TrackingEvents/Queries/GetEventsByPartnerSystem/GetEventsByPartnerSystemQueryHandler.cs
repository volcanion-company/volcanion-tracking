using MediatR;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;

namespace VolcanionTracking.Application.TrackingEvents.Queries.GetEventsByPartnerSystem;

public class GetEventsByPartnerSystemQueryHandler 
    : IRequestHandler<GetEventsByPartnerSystemQuery, GetEventsByPartnerSystemResult>
{
    private readonly ITrackingEventReadRepository _readRepository;
    private readonly ILogger<GetEventsByPartnerSystemQueryHandler> _logger;

    public GetEventsByPartnerSystemQueryHandler(
        ITrackingEventReadRepository readRepository,
        ILogger<GetEventsByPartnerSystemQueryHandler> logger)
    {
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<GetEventsByPartnerSystemResult> Handle(
        GetEventsByPartnerSystemQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Querying events for partner system: {PartnerSystemId}", 
            request.PartnerSystemId);

        // Query read model (optimized for analytics)
        var events = await _readRepository.GetByPartnerSystemIdAsync(
            request.PartnerSystemId,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalCount = await _readRepository.CountByPartnerSystemIdAsync(
            request.PartnerSystemId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var eventDtos = events.Select(e => new TrackingEventDto(
            e.Id,
            e.PartnerSystemId,
            e.PartnerId,
            e.PartnerName,
            e.SystemName,
            e.EventName,
            e.EventTimestamp,
            e.UserId,
            e.AnonymousId,
            e.EventPropertiesJson,
            e.IsValid,
            e.ValidationErrors,
            e.CorrelationId)).ToList();

        return new GetEventsByPartnerSystemResult(
            eventDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
