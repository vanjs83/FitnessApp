using FitnessApp.Application.DTOs.Chat;
using FluentValidation;

namespace FitnessApp.Application.Features.Chat.Commands;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}
