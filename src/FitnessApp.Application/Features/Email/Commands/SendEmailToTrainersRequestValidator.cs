using FitnessApp.Application.DTOs.Email;
using FluentValidation;

namespace FitnessApp.Application.Features.Email.Commands;

public class SendEmailToTrainersRequestValidator : AbstractValidator<SendEmailToTrainersRequest>
{
    public SendEmailToTrainersRequestValidator()
    {
        RuleFor(x => x.TrainerIds)
            .NotEmpty()
            .WithMessage("At least one trainer must be selected.");
        RuleForEach(x => x.TrainerIds).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10000);
    }
}
