using FitnessApp.Application.DTOs.Trainers;
using FluentValidation;

namespace FitnessApp.Application.Features.TrainerRequests.Commands;

public class CreateTrainerRequestRequestValidator : AbstractValidator<CreateTrainerRequestRequest>
{
    public CreateTrainerRequestRequestValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty().MaximumLength(450);
    }
}
