using FitnessApp.Application.DTOs.TrainingPlans;
using FluentValidation;

namespace FitnessApp.Application.Validators.TrainingPlans;

public class AddPlannedExerciseRequestValidator : AbstractValidator<AddPlannedExerciseRequest>
{
    public AddPlannedExerciseRequestValidator()
    {
        RuleFor(x => x.ExerciseId).GreaterThan(0);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetSets).InclusiveBetween(1, 50);
        RuleFor(x => x.TargetReps).InclusiveBetween(0, 1000);
        RuleFor(x => x.TargetWeightKg).InclusiveBetween(0m, 9999m);
        RuleFor(x => x.TargetDurationSeconds).InclusiveBetween(1, 7200).When(x => x.TargetDurationSeconds.HasValue);
        RuleFor(x => x.RestSeconds).InclusiveBetween(0, 3600).When(x => x.RestSeconds.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
