using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Validators.Shared;
using FitnessApp.Domain.Common;
using FluentValidation;

namespace FitnessApp.Application.Features.Auth.Commands;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();

        RuleFor(x => x.Password).ValidPassword();

        RuleFor(x => x.FullName).MaximumLength(100);

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => Roles.SelfRegisterable.Contains(r))
            .WithMessage("Role is not allowed for self-registration.");

        // Clients no longer pick a trainer at registration — they send a request
        // from their profile afterwards, which the trainer must accept.
    }
}
