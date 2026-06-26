using FitnessApp.Application.DTOs.Support;
using FluentValidation;

namespace FitnessApp.Application.Features.Support.Commands;

public class SupportContactRequestValidator : AbstractValidator<SupportContactRequest>
{
    public SupportContactRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
    }
}
