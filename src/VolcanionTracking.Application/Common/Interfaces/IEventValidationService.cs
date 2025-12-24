namespace VolcanionTracking.Application.Common.Interfaces;

/// <summary>
/// Event validation service interface
/// </summary>
public interface IEventValidationService
{
    Task<(bool IsValid, string? Errors)> ValidateEventAsync(
        Guid partnerSystemId,
        string eventName,
        string eventPropertiesJson,
        CancellationToken cancellationToken = default);
}
