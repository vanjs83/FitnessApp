using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Validators.Shared;
using FluentValidation;

namespace FitnessApp.Application.Features.Auth.Commands;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).NotEmpty();
    }
}
