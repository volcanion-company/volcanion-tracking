using Microsoft.Extensions.Logging;
using System.Text.Json;
using VolcanionTracking.Application.Common.Interfaces;

namespace VolcanionTracking.Infrastructure.Services;

/// <summary>
/// Event validation service - validates tracking events against schemas
/// In MVP, this is simplified. In production, consider JSON Schema validation
/// </summary>
public class EventValidationService : IEventValidationService
{
    private readonly ILogger<EventValidationService> _logger;
    private readonly ICacheService _cacheService;

    public EventValidationService(
        ILogger<EventValidationService> logger,
        ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<(bool IsValid, string? Errors)> ValidateEventAsync(
        Guid partnerSystemId,
        string eventName,
        string eventPropertiesJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic JSON validation
            var jsonDocument = JsonDocument.Parse(eventPropertiesJson);

            // In MVP, we do lightweight validation
            // In production, you would:
            // 1. Fetch PartnerEventStructure or EventStructure from cache
            // 2. Validate against JSON schema
            // 3. Check required properties, data types, etc.

            // For now, just validate it's valid JSON and reasonable size
            if (jsonDocument.RootElement.GetRawText().Length > 100_000)
            {
                return (false, "Event properties exceed maximum size of 100KB");
            }

            return (true, null);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in event properties for event: {EventName}", eventName);
            return (false, $"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating event: {EventName}", eventName);
            return (false, $"Validation error: {ex.Message}");
        }
    }
}
