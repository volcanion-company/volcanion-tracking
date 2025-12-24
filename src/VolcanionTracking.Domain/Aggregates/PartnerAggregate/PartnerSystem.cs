using VolcanionTracking.Domain.Common;
using VolcanionTracking.Domain.ValueObjects;

namespace VolcanionTracking.Domain.Aggregates.PartnerAggregate;

/// <summary>
/// PartnerSystem entity - represents a data source (website, app, backend service) for a partner
/// </summary>
public class PartnerSystem : Entity
{
    public Guid PartnerId { get; private set; }
    public string Name { get; private set; }
    public SystemType Type { get; private set; }
    public string Description { get; private set; }
    public ApiKey ApiKey { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    // EF Core constructor
    private PartnerSystem() { }

    private PartnerSystem(Guid partnerId, string name, SystemType type, string description)
    {
        PartnerId = partnerId;
        Name = name;
        Type = type;
        Description = description;
        ApiKey = ValueObjects.ApiKey.Generate();
        IsActive = true;
    }

    public static Result<PartnerSystem> Create(Guid partnerId, string name, SystemType type, string description)
    {
        if (partnerId == Guid.Empty)
            return Result<PartnerSystem>.Failure("Partner ID is required");

        if (string.IsNullOrWhiteSpace(name))
            return Result<PartnerSystem>.Failure("System name is required");

        if (string.IsNullOrWhiteSpace(description))
            return Result<PartnerSystem>.Failure("System description is required");

        return Result<PartnerSystem>.Success(new PartnerSystem(partnerId, name, type, description));
    }

    public Result RegenerateApiKey()
    {
        if (!IsActive)
            return Result.Failure("Cannot regenerate API key for inactive system");

        ApiKey = ValueObjects.ApiKey.Generate();
        SetUpdatedAt();
        
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure("System is already deactivated");

        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
        SetUpdatedAt();
        
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
            return Result.Failure("System is already active");

        IsActive = true;
        DeactivatedAt = null;
        SetUpdatedAt();
        
        return Result.Success();
    }

    public Result UpdateInfo(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("System name is required");

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("System description is required");

        Name = name;
        Description = description;
        SetUpdatedAt();
        
        return Result.Success();
    }
}
