using MediatR;

namespace VolcanionTracking.Application.TrackingEvents.Commands.IngestEvent;

public record IngestEventCommand(
    string ApiKey,
    string EventName,
    DateTime EventTimestamp,
    string? UserId,
    string AnonymousId,
    string EventPropertiesJson,
    string CorrelationId) : IRequest<IngestEventResult>;

public record IngestEventResult(
    Guid EventId,
    bool IsValid,
    string? ValidationErrors,
    DateTime ReceivedAt);
