using FitnessApp.Application.DTOs.Exercises;
using FluentValidation;

namespace FitnessApp.Application.Features.Exercises.Commands;

public class CreateExerciseRequestValidator : AbstractValidator<CreateExerciseRequest>
{
    public CreateExerciseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.VideoUrl).MaximumLength(500);
        RuleFor(x => x.MuscleGroup).MaximumLength(60);
        RuleFor(x => x.Type).IsInEnum();
    }
}
