using VolcanionTracking.Domain.Common;
using System.Text.Json;

namespace VolcanionTracking.Domain.Aggregates.EventStructureAggregate;

/// <summary>
/// EventStructure aggregate root - defines the default schema for tracking events
/// </summary>
public class EventStructure : AggregateRoot
{
    public string EventName { get; private set; }
    public string Description { get; private set; }
    public string SchemaJson { get; private set; } // JSON schema definition
    public bool IsActive { get; private set; }

    // EF Core constructor
    private EventStructure() { }

    private EventStructure(string eventName, string description, string schemaJson)
    {
        EventName = eventName;
        Description = description;
        SchemaJson = schemaJson;
        IsActive = true;
    }

    public static Result<EventStructure> Create(string eventName, string description, string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return Result<EventStructure>.Failure("Event name is required");

        if (string.IsNullOrWhiteSpace(description))
            return Result<EventStructure>.Failure("Description is required");

        if (string.IsNullOrWhiteSpace(schemaJson))
            return Result<EventStructure>.Failure("Schema is required");

        // Validate JSON format
        if (!IsValidJson(schemaJson))
            return Result<EventStructure>.Failure("Invalid JSON schema format");

        var structure = new EventStructure(eventName, description, schemaJson);
        structure.AddDomainEvent(new EventStructureCreatedEvent(structure.Id, eventName));
        
        return Result<EventStructure>.Success(structure);
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
        
        AddDomainEvent(new EventStructureUpdatedEvent(Id, EventName));
        
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure("Event structure is already deactivated");

        IsActive = false;
        SetUpdatedAt();
        
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
            return Result.Failure("Event structure is already active");

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
