using VolcanionTracking.Domain.Common;
using System.Text.Json;

namespace VolcanionTracking.Domain.Aggregates.EventStructureAggregate;

/// <summary>
/// PartnerEventStructure entity - partner-specific override of default event structures
/// </summary>
public class PartnerEventStructure : Entity
{
    public Guid PartnerId { get; private set; }
    public Guid? EventStructureId { get; private set; } // null means custom event
    public string EventName { get; private set; }
    public string Description { get; private set; }
    public string SchemaJson { get; private set; }
    public bool IsActive { get; private set; }

    // EF Core constructor
    private PartnerEventStructure() { }

    private PartnerEventStructure(
        Guid partnerId, 
        Guid? eventStructureId, 
        string eventName, 
        string description, 
        string schemaJson)
    {
        PartnerId = partnerId;
        EventStructureId = eventStructureId;
        EventName = eventName;
        Description = description;
        SchemaJson = schemaJson;
        IsActive = true;
    }

    public static Result<PartnerEventStructure> Create(
        Guid partnerId,
        Guid? eventStructureId,
        string eventName,
        string description,
        string schemaJson)
    {
        if (partnerId == Guid.Empty)
            return Result<PartnerEventStructure>.Failure("Partner ID is required");

        if (string.IsNullOrWhiteSpace(eventName))
            return Result<PartnerEventStructure>.Failure("Event name is required");

        if (string.IsNullOrWhiteSpace(description))
            return Result<PartnerEventStructure>.Failure("Description is required");

        if (string.IsNullOrWhiteSpace(schemaJson))
            return Result<PartnerEventStructure>.Failure("Schema is required");

        if (!IsValidJson(schemaJson))
            return Result<PartnerEventStructure>.Failure("Invalid JSON schema format");

        return Result<PartnerEventStructure>.Success(
            new PartnerEventStructure(partnerId, eventStructureId, eventName, description, schemaJson));
    }

    public Result UpdateSchema(string description, string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("Description is required");

        if (string.IsNullOrWhiteSpace(schemaJson))
            return Result.Failure("Schema is required");

        if (!IsValidJson(schemaJson))
            return Result.Failure("Invalid JSON schema format");

        Description = description;
        SchemaJson = schemaJson;
        SetUpdatedAt();
        
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure("Partner event structure is already deactivated");

        IsActive = false;
        SetUpdatedAt();
        
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
            return Result.Failure("Partner event structure is already active");

        IsActive = true;
        SetUpdatedAt();
        
        return Result.Success();
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
