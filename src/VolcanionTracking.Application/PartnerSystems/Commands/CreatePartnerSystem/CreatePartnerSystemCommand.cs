using MediatR;
using VolcanionTracking.Domain.Aggregates.PartnerAggregate;

namespace VolcanionTracking.Application.PartnerSystems.Commands.CreatePartnerSystem;

public record CreatePartnerSystemCommand(
    Guid PartnerId,
    string Name,
    SystemType Type,
    string Description) : IRequest<CreatePartnerSystemResult>;

public record CreatePartnerSystemResult(
    Guid Id,
    Guid PartnerId,
    string Name,
    SystemType Type,
    string Description,
    string ApiKey,
    bool IsActive);
