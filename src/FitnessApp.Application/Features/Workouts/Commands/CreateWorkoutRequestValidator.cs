using FitnessApp.Application.DTOs.Workouts;
using FluentValidation;

namespace FitnessApp.Application.Features.Workouts.Commands;

public class CreateWorkoutRequestValidator : AbstractValidator<CreateWorkoutRequest>
{
    public CreateWorkoutRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 1000).When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
