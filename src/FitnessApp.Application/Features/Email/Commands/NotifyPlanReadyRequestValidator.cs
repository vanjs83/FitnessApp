using FitnessApp.Application.DTOs.Email;
using FluentValidation;

namespace FitnessApp.Application.Features.Email.Commands;

public class NotifyPlanReadyRequestValidator : AbstractValidator<NotifyPlanReadyRequest>
{
    private static readonly string[] AllowedPlanTypes = { "training", "nutrition" };

    public NotifyPlanReadyRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.PlanName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PlanType)
            .NotEmpty()
            .Must(t => AllowedPlanTypes.Contains(t))
            .WithMessage("Plan type must be 'training' or 'nutrition'.");
    }
}
