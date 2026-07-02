using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Messaging.Commands;

public record SendPushToUsersCommand(
    IReadOnlyList<string> UserIds,
    string Subject,
    string Body,
    DateTime? SendAtUtc) : IRequest<Result>;

public class SendPushToUsersCommandHandler : IRequestHandler<SendPushToUsersCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMessageScheduler _scheduler;

    public SendPushToUsersCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IMessageScheduler scheduler)
    {
        _db = db;
        _currentUser = currentUser;
        _scheduler = scheduler;
    }

    public async Task<Result> Handle(SendPushToUsersCommand request, CancellationToken cancellationToken)
    {
        await UserMessageEnqueuer.EnqueueAsync(
            _db, _scheduler, _currentUser.UserId, ScheduledMessageChannel.Push,
            request.UserIds, request.Subject, request.Body, request.SendAtUtc, cancellationToken);

        return Result.Success();
    }
}
