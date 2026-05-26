using FitnessApp.Application.DTOs.Nutrition;
using FluentValidation;

namespace FitnessApp.Application.Validators.Nutrition;

public class CreateNutritionTemplateRequestValidator : AbstractValidator<CreateNutritionTemplateRequest>
{
    public CreateNutritionTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
