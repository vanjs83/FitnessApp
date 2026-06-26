using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Application.Validators.Shared;
using FluentValidation;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

public class CloneTemplateToClientRequestValidator : AbstractValidator<CloneTemplateToClientRequest>
{
    public CloneTemplateToClientRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.TrainerExpectations).MaximumLength(2000);
        RuleFor(x => x.Price).ValidPrice();
        RuleFor(x => x.Currency).ValidCurrency();
    }
}
