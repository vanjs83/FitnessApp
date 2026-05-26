using FitnessApp.Application.DTOs.TrainingPlans;
using FluentValidation;

namespace FitnessApp.Application.Validators.TrainingPlans;

public class AddTrainingDayRequestValidator : AbstractValidator<AddTrainingDayRequest>
{
    public AddTrainingDayRequestValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
