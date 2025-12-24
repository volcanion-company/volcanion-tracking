using MediatR;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;
using VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;
using System.Diagnostics;

namespace VolcanionTracking.Application.TrackingEvents.Commands.IngestEvent;

/// <summary>
/// Handles ingest event commands by validating, tracking, and persisting events for partner systems.
/// </summary>
/// <remarks>The handler ensures that all events are tracked, even if validation fails. Event schema validation
/// errors are included in the result but do not prevent ingestion. Caching is used to improve performance of partner
/// system lookups. This handler is typically used in distributed systems to process incoming event data from external
/// partners.</remarks>
/// <param name="partnerSystemRepository">The repository used to retrieve partner system information based on API keys.</param>
/// <param name="trackingEventRepository">The repository used to persist tracking events to the data store.</param>
/// <param name="validationService">The service used to validate event schemas and collect validation errors.</param>
/// <param name="unitOfWork">The unit of work that manages transactional persistence of changes.</param>
/// <param name="cacheService">The cache service used to optimize partner system lookups by API key.</param>
/// <param name="logger">The logger used to record informational and diagnostic messages for event ingestion operations.</param>
public class IngestEventCommandHandler(
    IPartnerSystemRepository partnerSystemRepository,
    ITrackingEventRepository trackingEventRepository,
    IEventValidationService validationService,
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    ILogger<IngestEventCommandHandler> logger) : IRequestHandler<IngestEventCommand, IngestEventResult>
{
    /// <summary>
    /// Processes an ingest event command by validating the event, persisting it, and returning the result of the
    /// ingestion operation.
    /// </summary>
    /// <remarks>The event is always tracked, regardless of validation outcome. Event schema validation errors
    /// are included in the result but do not prevent ingestion. The method uses caching to optimize partner system
    /// lookups.</remarks>
    /// <param name="request">The ingest event command containing event details, partner system API key, and associated metadata. Cannot be
    /// null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IngestEventResult with the event
    /// identifier, validation status, validation errors, and the timestamp of ingestion.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the API key provided in the request is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the partner system is not active or if the event could not be created or persisted.</exception>
    public async Task<IngestEventResult> Handle(IngestEventCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Ingesting event: {EventName} with correlation ID: {CorrelationId}", request.EventName, request.CorrelationId);
        }

        // Get partner system (cached)
        var cacheKey = $"partner_system:apikey:{request.ApiKey}";
        var cachedSystemIdStr = await cacheService.GetAsync<string>(cacheKey, cancellationToken);

        if (cachedSystemIdStr == null || !Guid.TryParse(cachedSystemIdStr, out Guid partnerSystemId))
        {
            var system = await partnerSystemRepository.GetByApiKeyAsync(request.ApiKey, cancellationToken) ?? throw new UnauthorizedAccessException("Invalid API key");
            if (!system.IsActive)
            {
                throw new InvalidOperationException("Partner system is not active");
            }

            partnerSystemId = system.Id;
            await cacheService.SetAsync(cacheKey, partnerSystemId.ToString(), TimeSpan.FromHours(1), cancellationToken);
        }

        // Validate event schema
        var (isValid, errors) = await validationService.ValidateEventAsync(
            partnerSystemId,
            request.EventName,
            request.EventPropertiesJson,
            cancellationToken);

        // Create tracking event (append-only, never reject)
        var trackingEventResult = TrackingEvent.Create(
            partnerSystemId,
            request.EventName,
            request.EventTimestamp,
            request.UserId,
            request.AnonymousId,
            request.EventPropertiesJson,
            isValid,
            errors,
            request.CorrelationId);

        if (!trackingEventResult.IsSuccess)
        {
            throw new InvalidOperationException(trackingEventResult.Error);
        }

        var trackingEvent = trackingEventResult.Value!;

        // Persist to write database
        await trackingEventRepository.AddAsync(trackingEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Event ingested: {EventId} in {ElapsedMs}ms, Valid: {IsValid}", trackingEvent.Id, stopwatch.ElapsedMilliseconds, isValid);
        }

        return new IngestEventResult(trackingEvent.Id, isValid, errors, DateTime.UtcNow);
    }
}
