using FitnessApp.Application.DTOs.Email;
using FluentValidation;

namespace FitnessApp.Application.Features.Email.Commands;

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
        RuleFor(x => x.SendAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.SendAtUtc.HasValue)
            .WithMessage("The send time must be in the future.");
    }
}
