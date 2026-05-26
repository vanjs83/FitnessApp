using FitnessApp.Application.DTOs.TrainingPlans;
using FluentValidation;

namespace FitnessApp.Application.Validators.TrainingPlans;

public class UpdateTrainingPlanTemplateRequestValidator : AbstractValidator<UpdateTrainingPlanTemplateRequest>
{
    public UpdateTrainingPlanTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.TrainerExpectations).MaximumLength(2000);
    }
}
