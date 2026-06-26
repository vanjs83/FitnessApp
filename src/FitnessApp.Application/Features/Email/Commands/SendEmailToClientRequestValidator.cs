using FitnessApp.Application.DTOs.Email;
using FluentValidation;

namespace FitnessApp.Application.Features.Email.Commands;

public class SendEmailToClientRequestValidator : AbstractValidator<SendEmailToClientRequest>
{
    public SendEmailToClientRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10000);
    }
}
