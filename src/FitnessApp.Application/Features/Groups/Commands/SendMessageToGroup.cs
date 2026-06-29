using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Application.Features.Messaging.Commands;
using MediatR;

namespace FitnessApp.Application.Features.Groups.Commands;

/// <summary>
/// Broadcasts a message to every member of one of the trainer's groups, over the chosen
/// channels. Resolves the group to its member ids and reuses the existing per-user
/// email/push commands so the delivery logic lives in one place.
/// </summary>
public record SendMessageToGroupCommand(
    int GroupId,
    string Subject,
    string Body,
    bool Email,
    bool Push) : IRequest<Result<GroupMessageResultDto>>;

public class SendMessageToGroupCommandHandler : IRequestHandler<SendMessageToGroupCommand, Result<GroupMessageResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public SendMessageToGroupCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ISender sender)
    {
        _db = db;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<Result<GroupMessageResultDto>> Handle(SendMessageToGroupCommand request, CancellationToken cancellationToken)
    {
        var (group, error) = await GroupGuard.LoadOwnedAsync(_db, request.GroupId, _currentUser.UserId, cancellationToken);
        if (error is not null) return Result<GroupMessageResultDto>.Fail(error.Value);

        var memberIds = group!.Members.Select(m => m.ClientId).Distinct().ToList();
        if (memberIds.Count == 0)
            return Result<GroupMessageResultDto>.Fail(ResultError.Validation, "The group has no members.");

        var result = new GroupMessageResultDto();

        if (request.Email)
        {
            var email = await _sender.Send(new SendEmailToUsersCommand(memberIds, request.Subject, request.Body), cancellationToken);
            result.Email = email.Value;
        }

        if (request.Push)
        {
            var push = await _sender.Send(new SendPushToUsersCommand(memberIds, request.Subject, request.Body), cancellationToken);
            result.Push = push.Value;
        }

        return Result<GroupMessageResultDto>.Success(result);
    }
}
