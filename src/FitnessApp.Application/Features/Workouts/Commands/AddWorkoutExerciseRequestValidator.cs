using FitnessApp.Application.DTOs.Workouts;
using FluentValidation;

namespace FitnessApp.Application.Features.Workouts.Commands;

public class AddWorkoutExerciseRequestValidator : AbstractValidator<AddWorkoutExerciseRequest>
{
    public AddWorkoutExerciseRequestValidator()
    {
        RuleFor(x => x.ExerciseId).GreaterThan(0);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
