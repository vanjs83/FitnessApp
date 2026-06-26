using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Validators.Shared;
using FluentValidation;

namespace FitnessApp.Application.Features.Auth.Commands;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).ValidPassword();
    }
}
