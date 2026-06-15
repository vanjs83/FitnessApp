using FitnessApp.Application.DTOs.Auth;
using FluentValidation;

namespace FitnessApp.Application.Validators.Auth;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
