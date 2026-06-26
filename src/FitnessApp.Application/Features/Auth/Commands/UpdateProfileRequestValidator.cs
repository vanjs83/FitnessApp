using FitnessApp.Application.DTOs.Auth;
using FluentValidation;

namespace FitnessApp.Application.Features.Auth.Commands;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(120);
    }
}
