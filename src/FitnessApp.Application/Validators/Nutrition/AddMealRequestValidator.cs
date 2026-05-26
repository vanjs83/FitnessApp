using FitnessApp.Application.DTOs.Nutrition;
using FluentValidation;

namespace FitnessApp.Application.Validators.Nutrition;

public class AddMealRequestValidator : AbstractValidator<AddMealRequest>
{
    public AddMealRequestValidator()
    {
        RuleFor(x => x.MealType).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
