using VolcanionTracking.Domain.Common;

namespace VolcanionTracking.Domain.Aggregates.PartnerAggregate;

/// <summary>
/// Partner aggregate root - represents a client/customer that uses the tracking system
/// </summary>
public class Partner : AggregateRoot
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    private readonly List<PartnerSystem> _systems = new();
    public IReadOnlyCollection<PartnerSystem> Systems => _systems.AsReadOnly();

    // EF Core constructor
    private Partner() { }

    private Partner(string name, string email)
    {
        Name = name;
        Email = email;
        IsActive = true;
    }

    public static Result<Partner> Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Partner>.Failure("Partner name is required");

        if (string.IsNullOrWhiteSpace(email))
            return Result<Partner>.Failure("Partner email is required");

        if (!IsValidEmail(email))
            return Result<Partner>.Failure("Invalid email format");

        var partner = new Partner(name, email);
        partner.AddDomainEvent(new PartnerCreatedEvent(partner.Id, partner.Name));
        
        return Result<Partner>.Success(partner);
    }

    public Result<PartnerSystem> AddSystem(string name, SystemType type, string description)
    {
        if (!IsActive)
            return Result<PartnerSystem>.Failure("Cannot add system to inactive partner");

        var systemResult = PartnerSystem.Create(Id, name, type, description);
        if (!systemResult.IsSuccess)
            return systemResult;

        _systems.Add(systemResult.Value!);
        SetUpdatedAt();
        
        AddDomainEvent(new PartnerSystemAddedEvent(Id, systemResult.Value!.Id, name, type));
        
        return systemResult;
    }

    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure("Partner is already deactivated");

        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
        SetUpdatedAt();
        
        AddDomainEvent(new PartnerDeactivatedEvent(Id, Name));
        
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
            return Result.Failure("Partner is already active");

        IsActive = true;
        DeactivatedAt = null;
        SetUpdatedAt();
        
        AddDomainEvent(new PartnerActivatedEvent(Id, Name));
        
        return Result.Success();
    }

    public Result UpdateInfo(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Partner name is required");

        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure("Partner email is required");

        if (!IsValidEmail(email))
            return Result.Failure("Invalid email format");

        Name = name;
        Email = email;
        SetUpdatedAt();
        
        return Result.Success();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
