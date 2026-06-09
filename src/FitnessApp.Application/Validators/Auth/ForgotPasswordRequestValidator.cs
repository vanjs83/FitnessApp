using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Validators.Shared;
using FluentValidation;

namespace FitnessApp.Application.Validators.Auth;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
    }
}
