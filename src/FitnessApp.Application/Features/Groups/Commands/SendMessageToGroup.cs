using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Groups.Commands;

/// <summary>
/// Broadcasts a message to every member of one of the trainer's groups over the chosen channels.
/// Queues one <see cref="ScheduledMessage"/> per channel (Audience=Group, delivered now) into the
/// outbox; the recurring DueScheduledMessagesJob resolves members and delivers, so this returns
/// without waiting on SMTP/Firebase. A due-scan is kicked immediately so "now" isn't held for the
/// next cron tick.
/// </summary>
public record SendMessageToGroupCommand(
    int GroupId,
    string Subject,
    string Body,
    bool Email,
    bool Push) : IRequest<Result>;

public class SendMessageToGroupCommandHandler : IRequestHandler<SendMessageToGroupCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMessageScheduler _scheduler;

    public SendMessageToGroupCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IMessageScheduler scheduler)
    {
        _db = db;
        _currentUser = currentUser;
        _scheduler = scheduler;
    }

    public async Task<Result> Handle(SendMessageToGroupCommand request, CancellationToken cancellationToken)
    {
        if (!request.Email && !request.Push)
            return Result.Fail(ResultError.Validation, "Select at least one channel.");

        var (group, error) = await GroupGuard.LoadOwnedAsync(_db, request.GroupId, _currentUser.UserId, cancellationToken);
        if (error is not null) return Result.Fail(error.Value);

        if (group!.Members.Count == 0)
            return Result.Fail(ResultError.Validation, "The group has no members.");

        var now = DateTime.UtcNow;
        if (request.Email) QueueChannel(ScheduledMessageChannel.Email, request, now);
        if (request.Push) QueueChannel(ScheduledMessageChannel.Push, request, now);

        await _db.SaveChangesAsync(cancellationToken);

        _scheduler.DispatchDueMessagesNow();

        return Result.Success();
    }

    private void QueueChannel(ScheduledMessageChannel channel, SendMessageToGroupCommand request, DateTime now) =>
        _db.ScheduledMessages.Add(new ScheduledMessage
        {
            SenderId = _currentUser.UserId,
            Channel = channel,
            Audience = ScheduledMessageAudience.Group,
            GroupId = request.GroupId,
            Subject = request.Subject,
            Body = request.Body,
            SendAtUtc = now,
            Status = ScheduledMessageStatus.Pending,
            CreatedAtUtc = now
        });
}
