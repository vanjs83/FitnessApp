using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Domain.Common;
using FluentValidation;

namespace FitnessApp.Application.Validators.Auth;

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.Credential)
            .NotEmpty()
            .MaximumLength(5000);

        // Role is optional (only sent from the Register tab); when present it must
        // be a role the user is allowed to self-register as.
        When(x => !string.IsNullOrEmpty(x.Role), () =>
        {
            RuleFor(x => x.Role)
                .Must(r => Roles.SelfRegisterable.Contains(r!))
                .WithMessage("Role is not allowed for self-registration.");
        });
    }
}
