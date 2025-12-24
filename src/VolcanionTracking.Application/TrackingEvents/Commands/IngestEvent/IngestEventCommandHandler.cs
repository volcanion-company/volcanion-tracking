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
        var cachedSystemIdStr = await _cacheService.GetAsync<string>(cacheKey, cancellationToken);
        Guid partnerSystemId;
        
        if (cachedSystemIdStr == null || !Guid.TryParse(cachedSystemIdStr, out partnerSystemId))
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

            partnerSystemId = system.Id;
            await _cacheService.SetAsync(cacheKey, partnerSystemId.ToString(), TimeSpan.FromHours(1), cancellationToken);
        }

        // Validate event schema
        var validationResult = await _validationService.ValidateEventAsync(
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
            validationResult.IsValid,
            validationResult.Errors,
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
            validationResult.IsValid);

        return new IngestEventResult(
            trackingEvent.Id,
            validationResult.IsValid,
            validationResult.Errors,
            DateTime.UtcNow);
    }
}
