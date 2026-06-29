using FitnessApp.Application.DTOs.Groups;
using FluentValidation;

namespace FitnessApp.Application.Features.Groups.Commands;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ClientIds).NotNull();
        RuleForEach(x => x.ClientIds).NotEmpty();
    }
}

public class AddGroupMemberRequestValidator : AbstractValidator<AddGroupMemberRequest>
{
    public AddGroupMemberRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
    }
}

public class SendMessageToGroupRequestValidator : AbstractValidator<SendMessageToGroupRequest>
{
    public SendMessageToGroupRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10000);
        RuleFor(x => x).Must(x => x.Email || x.Push)
            .WithMessage("Select at least one channel (email or push).");
    }
}
