using FitnessApp.Application.DTOs.Email;
using FluentValidation;

namespace FitnessApp.Application.Validators.Email;

public class SendMessageToUsersRequestValidator : AbstractValidator<SendMessageToUsersRequest>
{
    public SendMessageToUsersRequestValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty()
            .WithMessage("At least one user must be selected.");
        RuleForEach(x => x.UserIds).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10000);
    }
}
