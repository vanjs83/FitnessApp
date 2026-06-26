using FitnessApp.Application.DTOs.TrainingPlans;
using FluentValidation;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

public class LogPerformedSetRequestValidator : AbstractValidator<LogPerformedSetRequest>
{
    public LogPerformedSetRequestValidator()
    {
        RuleFor(x => x.SetNumber).InclusiveBetween(1, 50);
        RuleFor(x => x.ActualReps).InclusiveBetween(1, 1000);
        RuleFor(x => x.ActualWeightKg).InclusiveBetween(0m, 9999m);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
