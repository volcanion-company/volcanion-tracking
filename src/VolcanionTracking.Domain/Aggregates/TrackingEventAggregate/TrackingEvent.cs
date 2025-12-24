using VolcanionTracking.Domain.Common;
using System.Text.Json;

namespace VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;

/// <summary>
/// TrackingEvent aggregate root - Write model for event ingestion
/// Optimized for high-throughput writes
/// </summary>
public class TrackingEvent : AggregateRoot
{
    public Guid PartnerSystemId { get; private set; }
    public string EventName { get; private set; }
    public DateTime EventTimestamp { get; private set; }
    public string? UserId { get; private set; }
    public string AnonymousId { get; private set; }
    public string EventPropertiesJson { get; private set; } // JSONB storage
    public bool IsValid { get; private set; }
    public string? ValidationErrors { get; private set; }
    public string CorrelationId { get; private set; }

    // EF Core constructor
    private TrackingEvent() { }

    private TrackingEvent(
        Guid partnerSystemId,
        string eventName,
        DateTime eventTimestamp,
        string? userId,
        string anonymousId,
        string eventPropertiesJson,
        bool isValid,
        string? validationErrors,
        string correlationId)
    {
        PartnerSystemId = partnerSystemId;
        EventName = eventName;
        EventTimestamp = eventTimestamp;
        UserId = userId;
        AnonymousId = anonymousId;
        EventPropertiesJson = eventPropertiesJson;
        IsValid = isValid;
        ValidationErrors = validationErrors;
        CorrelationId = correlationId;
    }

    public static Result<TrackingEvent> Create(
        Guid partnerSystemId,
        string eventName,
        DateTime eventTimestamp,
        string? userId,
        string anonymousId,
        string eventPropertiesJson,
        bool isValid,
        string? validationErrors,
        string correlationId)
    {
        if (partnerSystemId == Guid.Empty)
            return Result<TrackingEvent>.Failure("Partner system ID is required");

        if (string.IsNullOrWhiteSpace(eventName))
            return Result<TrackingEvent>.Failure("Event name is required");

        if (string.IsNullOrWhiteSpace(anonymousId))
            return Result<TrackingEvent>.Failure("Anonymous ID is required");

        if (string.IsNullOrWhiteSpace(eventPropertiesJson))
            eventPropertiesJson = "{}";

        if (!IsValidJson(eventPropertiesJson))
            return Result<TrackingEvent>.Failure("Invalid event properties JSON format");

        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString();

        var trackingEvent = new TrackingEvent(
            partnerSystemId,
            eventName,
            eventTimestamp,
            userId,
            anonymousId,
            eventPropertiesJson,
            isValid,
            validationErrors,
            correlationId);

        trackingEvent.AddDomainEvent(new TrackingEventReceivedEvent(
            trackingEvent.Id,
            partnerSystemId,
            eventName,
            isValid));

        return Result<TrackingEvent>.Success(trackingEvent);
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
