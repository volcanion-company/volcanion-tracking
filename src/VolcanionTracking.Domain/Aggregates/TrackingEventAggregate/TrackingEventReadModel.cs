using VolcanionTracking.Domain.Common;

namespace VolcanionTracking.Domain.Aggregates.TrackingEventAggregate;

/// <summary>
/// TrackingEventReadModel - Read model optimized for analytics queries
/// Denormalized for fast reads
/// </summary>
public class TrackingEventReadModel : Entity
{
    public Guid TrackingEventId { get; set; }
    public Guid PartnerSystemId { get; set; }
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; }
    public string SystemName { get; set; }
    public string EventName { get; set; }
    public DateTime EventTimestamp { get; set; }
    public string? UserId { get; set; }
    public string AnonymousId { get; set; }
    public string EventPropertiesJson { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationErrors { get; set; }
    public string CorrelationId { get; set; }
    public DateTime ProcessedAt { get; set; }

    // EF Core constructor
    private TrackingEventReadModel() { }

    public TrackingEventReadModel(
        Guid trackingEventId,
        Guid partnerSystemId,
        Guid partnerId,
        string partnerName,
        string systemName,
        string eventName,
        DateTime eventTimestamp,
        string? userId,
        string anonymousId,
        string eventPropertiesJson,
        bool isValid,
        string? validationErrors,
        string correlationId)
    {
        TrackingEventId = trackingEventId;
        PartnerSystemId = partnerSystemId;
        PartnerId = partnerId;
        PartnerName = partnerName;
        SystemName = systemName;
        EventName = eventName;
        EventTimestamp = eventTimestamp;
        UserId = userId;
        AnonymousId = anonymousId;
        EventPropertiesJson = eventPropertiesJson;
        IsValid = isValid;
        ValidationErrors = validationErrors;
        CorrelationId = correlationId;
        ProcessedAt = DateTime.UtcNow;
    }
}
