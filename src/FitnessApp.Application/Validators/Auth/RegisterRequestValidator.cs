using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Validators.Shared;
using FitnessApp.Domain.Common;
using FluentValidation;

namespace FitnessApp.Application.Validators.Auth;

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

        When(x => x.Role == Roles.Client, () =>
        {
            RuleFor(x => x.TrainerId)
                .NotEmpty()
                .WithMessage("Client must select a trainer.");
        });
    }
}
