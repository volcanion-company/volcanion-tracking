using MediatR;

namespace VolcanionTracking.Application.Partners.Commands.CreatePartner;

public record CreatePartnerCommand(
    string Code,
    string Name,
    string Email,
    string AESKey,
    string RSAPublicKey,
    string RSAPrivateKey) : IRequest<CreatePartnerResult>;

public record CreatePartnerResult(
    Guid Id,
    string Code,
    string Name,
    string Email,
    bool IsActive);
