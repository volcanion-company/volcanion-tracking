using FluentValidation;

namespace VolcanionTracking.Application.Partners.Commands.CreatePartner;

public class CreatePartnerCommandValidator : AbstractValidator<CreatePartnerCommand>
{
    public CreatePartnerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Partner name is required")
            .MaximumLength(200).WithMessage("Partner name must not exceed 200 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
    }
}
