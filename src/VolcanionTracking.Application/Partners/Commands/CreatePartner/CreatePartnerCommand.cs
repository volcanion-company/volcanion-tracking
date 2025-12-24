using MediatR;

namespace VolcanionTracking.Application.Partners.Commands.CreatePartner;

public record CreatePartnerCommand(
    string Name,
    string Email) : IRequest<CreatePartnerResult>;

public record CreatePartnerResult(
    Guid Id,
    string Name,
    string Email,
    bool IsActive);
