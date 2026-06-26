using FitnessApp.Application.DTOs.Nutrition;
using FluentValidation;

namespace FitnessApp.Application.Features.Nutrition.Commands;

public class AddNutritionDayRequestValidator : AbstractValidator<AddNutritionDayRequest>
{
    public AddNutritionDayRequestValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(60);
        RuleFor(x => x.TotalCaloriesTarget).InclusiveBetween(0, 20000).When(x => x.TotalCaloriesTarget.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
