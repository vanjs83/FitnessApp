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
