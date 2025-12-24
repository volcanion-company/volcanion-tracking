using FluentValidation;

namespace VolcanionTracking.Application.Partners.Commands.CreatePartner;

public class CreatePartnerCommandValidator : AbstractValidator<CreatePartnerCommand>
{
    public CreatePartnerCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Partner code is required")
            .MaximumLength(50).WithMessage("Partner code must not exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Partner name is required")
            .MaximumLength(200).WithMessage("Partner name must not exceed 200 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.AESKey)
            .NotEmpty().WithMessage("AES key is required")
            .MaximumLength(500).WithMessage("AES key must not exceed 500 characters");

        RuleFor(x => x.RSAPublicKey)
            .NotEmpty().WithMessage("RSA public key is required")
            .MaximumLength(4000).WithMessage("RSA public key must not exceed 4000 characters");

        RuleFor(x => x.RSAPrivateKey)
            .NotEmpty().WithMessage("RSA private key is required")
            .MaximumLength(4000).WithMessage("RSA private key must not exceed 4000 characters");
    }
}
