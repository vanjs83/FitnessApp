using FitnessApp.Application.DTOs.Chat;
using FluentValidation;

namespace FitnessApp.Application.Validators.Chat;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}
