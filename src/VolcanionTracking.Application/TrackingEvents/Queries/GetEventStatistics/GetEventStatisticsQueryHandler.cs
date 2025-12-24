using MediatR;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;

namespace VolcanionTracking.Application.TrackingEvents.Queries.GetEventStatistics;

public class GetEventStatisticsQueryHandler 
    : IRequestHandler<GetEventStatisticsQuery, EventStatisticsResult>
{
    private readonly ITrackingEventReadRepository _readRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetEventStatisticsQueryHandler> _logger;

    public GetEventStatisticsQueryHandler(
        ITrackingEventReadRepository readRepository,
        ICacheService cacheService,
        ILogger<GetEventStatisticsQueryHandler> logger)
    {
        _readRepository = readRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<EventStatisticsResult> Handle(
        GetEventStatisticsQuery request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting statistics for partner system: {PartnerSystemId}", 
            request.PartnerSystemId);

        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        // Check cache first
        var cacheKey = $"stats:{request.PartnerSystemId}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
        var cachedStats = await _cacheService.GetAsync<EventStatisticsResult>(cacheKey, cancellationToken);
        if (cachedStats != null)
        {
            _logger.LogInformation("Returning cached statistics");
            return cachedStats;
        }

        // Query from read model
        var totalCount = await _readRepository.CountByPartnerSystemIdAsync(
            request.PartnerSystemId,
            startDate,
            endDate,
            cancellationToken);

        // For MVP, we'll return simplified stats
        // In production, you'd aggregate by event name using specialized queries
        var stats = new EventStatisticsResult(
            request.PartnerSystemId,
            totalCount,
            totalCount, // Placeholder
            0, // Placeholder
            new Dictionary<string, long>(), // Placeholder
            startDate,
            endDate);

        // Cache for 5 minutes
        await _cacheService.SetAsync(cacheKey, stats, TimeSpan.FromMinutes(5), cancellationToken);

        return stats;
    }
}
