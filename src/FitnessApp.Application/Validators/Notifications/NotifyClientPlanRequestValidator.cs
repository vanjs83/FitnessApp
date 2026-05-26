using FitnessApp.Application.DTOs.Notifications;
using FluentValidation;

namespace FitnessApp.Application.Validators.Notifications;

public class NotifyClientPlanRequestValidator : AbstractValidator<NotifyClientPlanRequest>
{
    private static readonly string[] AllowedPlanTypes = { "training", "nutrition" };

    public NotifyClientPlanRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.PlanName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PlanType)
            .NotEmpty()
            .Must(t => AllowedPlanTypes.Contains(t))
            .WithMessage("Plan type must be 'training' or 'nutrition'.");
    }
}
