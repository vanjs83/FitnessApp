using FitnessApp.Application.DTOs.Workouts;
using FluentValidation;

namespace FitnessApp.Application.Validators.Workouts;

public class AddWorkoutSetRequestValidator : AbstractValidator<AddWorkoutSetRequest>
{
    public AddWorkoutSetRequestValidator()
    {
        RuleFor(x => x.WorkoutExerciseId).GreaterThan(0);
        RuleFor(x => x.SetNumber).InclusiveBetween(1, 50);
        RuleFor(x => x.Weight).InclusiveBetween(0m, 9999m);
        RuleFor(x => x.Reps).InclusiveBetween(0, 1000);
    }
}
