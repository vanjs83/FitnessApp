using FitnessApp.Application.DTOs.Auth;
using FluentValidation;

namespace FitnessApp.Application.Validators.Auth;

public class UpdatePersonalProfileRequestValidator : AbstractValidator<UpdatePersonalProfileRequest>
{
    public UpdatePersonalProfileRequestValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(120);
        RuleFor(x => x.Gender).MaximumLength(10);
        RuleFor(x => x.HeightCm).InclusiveBetween(50, 260).When(x => x.HeightCm.HasValue);
        RuleFor(x => x.WeightKg).InclusiveBetween(20m, 400m).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.Goal).MaximumLength(500);
        RuleFor(x => x.HealthNotes).MaximumLength(1000);
        RuleFor(x => x.ActivityLevel).MaximumLength(20);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.PreferredWeeklyTrainingCount).InclusiveBetween(1, 14).When(x => x.PreferredWeeklyTrainingCount.HasValue);
    }
}
