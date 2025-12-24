using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;
using VolcanionTracking.Infrastructure.Persistence;

namespace VolcanionTracking.Infrastructure.BackgroundServices;

/// <summary>
/// Background service to sync events from Write DB to Read DB
/// This implements the eventual consistency pattern for CQRS
/// 
/// PRODUCTION ALTERNATIVES:
/// - Change Data Capture (CDC) with Debezium
/// - PostgreSQL Logical Replication
/// - Message Queue (RabbitMQ, Azure Service Bus)
/// - Event Sourcing with projections
/// </summary>
public class EventSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventSyncBackgroundService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private DateTime _lastProcessedTimestamp = DateTime.MinValue;

    public EventSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EventSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event Sync Background Service started");

        // Wait a bit before starting to ensure DB is ready
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing events from Write DB to Read DB");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Event Sync Background Service stopped");
    }

    private async Task SyncEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var writeContext = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        var readContext = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        // Query new events from Write DB
        var newEvents = await writeContext.Set<TrackingEvent>()
            .Where(e => e.CreatedAt > _lastProcessedTimestamp)
            .OrderBy(e => e.CreatedAt)
            .Take(1000) // Batch size
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!newEvents.Any())
        {
            return;
        }

        _logger.LogInformation(
            "Processing {Count} new events from Write DB", 
            newEvents.Count);

        // For each event, create a denormalized read model
        var readModels = new List<TrackingEventReadModel>();

        foreach (var trackingEvent in newEvents)
        {
            // Get PartnerSystem with Partner info (this could be optimized with a join)
            var partnerSystem = await writeContext.Set<Domain.Aggregates.PartnerAggregate.PartnerSystem>()
                .Include(ps => ps.PartnerId) // This would need proper navigation
                .FirstOrDefaultAsync(ps => ps.Id == trackingEvent.PartnerSystemId, cancellationToken);

            if (partnerSystem == null)
            {
                _logger.LogWarning(
                    "PartnerSystem {SystemId} not found for event {EventId}", 
                    trackingEvent.PartnerSystemId, 
                    trackingEvent.Id);
                continue;
            }

            // Get Partner info
            var partner = await writeContext.Set<Domain.Aggregates.PartnerAggregate.Partner>()
                .FirstOrDefaultAsync(p => p.Id == partnerSystem.PartnerId, cancellationToken);

            if (partner == null)
            {
                _logger.LogWarning(
                    "Partner {PartnerId} not found for event {EventId}", 
                    partnerSystem.PartnerId, 
                    trackingEvent.Id);
                continue;
            }

            // Create denormalized read model
            var readModel = new TrackingEventReadModel(
                trackingEvent.Id,
                partnerSystem.Id,
                partner.Id,
                partner.Name,
                partnerSystem.Name,
                trackingEvent.EventName,
                trackingEvent.EventTimestamp,
                trackingEvent.UserId,
                trackingEvent.AnonymousId,
                trackingEvent.EventPropertiesJson,
                trackingEvent.IsValid,
                trackingEvent.ValidationErrors,
                trackingEvent.CorrelationId);

            readModels.Add(readModel);
        }

        // Bulk insert into Read DB
        if (readModels.Any())
        {
            await readContext.Set<TrackingEventReadModel>()
                .AddRangeAsync(readModels, cancellationToken);
            
            await readContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Synced {Count} events to Read DB", 
                readModels.Count);

            // Update last processed timestamp
            _lastProcessedTimestamp = newEvents.Max(e => e.CreatedAt);
        }
    }
}
