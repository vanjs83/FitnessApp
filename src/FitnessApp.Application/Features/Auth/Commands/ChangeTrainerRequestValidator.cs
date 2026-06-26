using FitnessApp.Application.DTOs.Auth;
using FluentValidation;

namespace FitnessApp.Application.Features.Auth.Commands;

public class ChangeTrainerRequestValidator : AbstractValidator<ChangeTrainerRequest>
{
    public ChangeTrainerRequestValidator()
    {
        // TrainerId is null when disconnecting; when present it must be a sane id.
        When(x => !string.IsNullOrEmpty(x.TrainerId), () =>
        {
            RuleFor(x => x.TrainerId).MaximumLength(450);
        });
    }
}
