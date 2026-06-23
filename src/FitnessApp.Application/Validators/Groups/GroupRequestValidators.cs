using FitnessApp.Application.DTOs.Groups;
using FluentValidation;

namespace FitnessApp.Application.Validators.Groups;

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

public class CreateGroupSessionRequestValidator : AbstractValidator<CreateGroupSessionRequest>
{
    public CreateGroupSessionRequestValidator()
    {
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Location).MaximumLength(400);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
