using MediatR;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;
using VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;
using System.Diagnostics;

namespace VolcanionTracking.Application.TrackingEvents.Commands.IngestEvent;

public class IngestEventCommandHandler : IRequestHandler<IngestEventCommand, IngestEventResult>
{
    private readonly IPartnerSystemRepository _partnerSystemRepository;
    private readonly ITrackingEventRepository _trackingEventRepository;
    private readonly IEventValidationService _validationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<IngestEventCommandHandler> _logger;

    public IngestEventCommandHandler(
        IPartnerSystemRepository partnerSystemRepository,
        ITrackingEventRepository trackingEventRepository,
        IEventValidationService validationService,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<IngestEventCommandHandler> logger)
    {
        _partnerSystemRepository = partnerSystemRepository;
        _trackingEventRepository = trackingEventRepository;
        _validationService = validationService;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IngestEventResult> Handle(IngestEventCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation(
            "Ingesting event: {EventName} with correlation ID: {CorrelationId}", 
            request.EventName, 
            request.CorrelationId);

        // Get partner system (cached)
        var cacheKey = $"partner_system:apikey:{request.ApiKey}";
        var partnerSystem = await _cacheService.GetAsync<Guid?>(cacheKey, cancellationToken);
        
        if (partnerSystem == null)
        {
            var system = await _partnerSystemRepository.GetByApiKeyAsync(request.ApiKey, cancellationToken);
            if (system == null)
            {
                throw new UnauthorizedAccessException("Invalid API key");
            }

            if (!system.IsActive)
            {
                throw new InvalidOperationException("Partner system is not active");
            }

            partnerSystem = system.Id;
            await _cacheService.SetAsync(cacheKey, partnerSystem, TimeSpan.FromHours(1), cancellationToken);
        }

        // Validate event schema
        var (isValid, validationErrors) = await _validationService.ValidateEventAsync(
            partnerSystem.Value,
            request.EventName,
            request.EventPropertiesJson,
            cancellationToken);

        // Create tracking event (append-only, never reject)
        var trackingEventResult = TrackingEvent.Create(
            partnerSystem.Value,
            request.EventName,
            request.EventTimestamp,
            request.UserId,
            request.AnonymousId,
            request.EventPropertiesJson,
            isValid,
            validationErrors,
            request.CorrelationId);

        if (!trackingEventResult.IsSuccess)
        {
            throw new InvalidOperationException(trackingEventResult.Error);
        }

        var trackingEvent = trackingEventResult.Value!;

        // Persist to write database
        await _trackingEventRepository.AddAsync(trackingEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        
        _logger.LogInformation(
            "Event ingested: {EventId} in {ElapsedMs}ms, Valid: {IsValid}", 
            trackingEvent.Id,
            stopwatch.ElapsedMilliseconds,
            isValid);

        return new IngestEventResult(
            trackingEvent.Id,
            isValid,
            validationErrors,
            DateTime.UtcNow);
    }
}
