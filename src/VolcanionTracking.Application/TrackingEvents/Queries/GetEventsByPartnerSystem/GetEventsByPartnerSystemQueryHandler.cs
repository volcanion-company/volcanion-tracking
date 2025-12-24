using MediatR;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;

namespace VolcanionTracking.Application.TrackingEvents.Queries.GetEventsByPartnerSystem;

/// <summary>
/// Handles queries to retrieve tracking events associated with a specified partner system, supporting optional date
/// range filtering and pagination.
/// </summary>
/// <remarks>This handler is optimized for analytics scenarios where large sets of tracking events may need to be
/// queried efficiently. It returns only events linked to the specified partner system and, if provided, within the
/// requested date range. Pagination parameters allow callers to retrieve results in manageable batches.</remarks>
/// <param name="readRepository">The repository used to read tracking event data from the underlying data source. Cannot be null.</param>
/// <param name="logger">The logger used to record diagnostic and operational information for this handler. Cannot be null.</param>
public class GetEventsByPartnerSystemQueryHandler(
    ITrackingEventReadRepository readRepository,
    ILogger<GetEventsByPartnerSystemQueryHandler> logger) : IRequestHandler<GetEventsByPartnerSystemQuery, GetEventsByPartnerSystemResult>
{
    /// <summary>
    /// Handles the retrieval of tracking events for a specified partner system, applying optional date range and
    /// pagination filters.
    /// </summary>
    /// <remarks>The returned result includes only events associated with the specified partner system and
    /// within the provided date range, if specified. The operation is optimized for analytics scenarios and supports
    /// paginated access to large event sets.</remarks>
    /// <param name="request">The query parameters specifying the partner system identifier, date range, and pagination options for retrieving
    /// events. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see
    /// cref="GetEventsByPartnerSystemResult"/> with the list of matching tracking events and pagination details.</returns>
    public async Task<GetEventsByPartnerSystemResult> Handle(
        GetEventsByPartnerSystemQuery request, 
        CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Querying events for partner system: {PartnerSystemId}", request.PartnerSystemId);
        }

        // Query read model (optimized for analytics)
        var events = await readRepository.GetByPartnerSystemIdAsync(
            request.PartnerSystemId,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalCount = await readRepository.CountByPartnerSystemIdAsync(
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
