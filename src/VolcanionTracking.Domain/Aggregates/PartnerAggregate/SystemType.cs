namespace VolcanionTracking.Domain.Aggregates.PartnerAggregate;

/// <summary>
/// Type of partner system (data source)
/// </summary>
public enum SystemType
{
    Website = 1,
    IosApp = 2,
    AndroidApp = 3,
    BackendService = 4,
    MobileWeb = 5,
    DesktopApp = 6,
    Other = 99
}
