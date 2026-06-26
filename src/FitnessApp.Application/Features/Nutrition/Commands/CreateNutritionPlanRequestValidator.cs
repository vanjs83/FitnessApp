using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Application.Validators.Shared;
using FluentValidation;

namespace FitnessApp.Application.Features.Nutrition.Commands;

public class CreateNutritionPlanRequestValidator : AbstractValidator<CreateNutritionPlanRequest>
{
    public CreateNutritionPlanRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");

        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.Price).ValidPrice();
        RuleFor(x => x.Currency).ValidCurrency();
    }
}
