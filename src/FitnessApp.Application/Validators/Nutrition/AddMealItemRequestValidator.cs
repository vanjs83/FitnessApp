using FitnessApp.Application.DTOs.Nutrition;
using FluentValidation;

namespace FitnessApp.Application.Validators.Nutrition;

public class AddMealItemRequestValidator : AbstractValidator<AddMealItemRequest>
{
    public AddMealItemRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Quantity).MaximumLength(60);
        RuleFor(x => x.Calories).InclusiveBetween(0, 20000).When(x => x.Calories.HasValue);
        RuleFor(x => x.ProteinG).InclusiveBetween(0m, 1000m).When(x => x.ProteinG.HasValue);
        RuleFor(x => x.CarbsG).InclusiveBetween(0m, 1000m).When(x => x.CarbsG.HasValue);
        RuleFor(x => x.FatG).InclusiveBetween(0m, 1000m).When(x => x.FatG.HasValue);
    }
}
