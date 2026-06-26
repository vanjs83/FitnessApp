using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Application.Validators.Shared;
using FluentValidation;

namespace FitnessApp.Application.Features.Trainers.Commands;

public class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.FullName).MaximumLength(120);
    }
}
