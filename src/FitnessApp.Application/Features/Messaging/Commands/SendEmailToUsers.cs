using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Messaging.Commands;

public record SendEmailToUsersCommand(
    IReadOnlyList<string> UserIds,
    string Subject,
    string Body,
    DateTime? SendAtUtc) : IRequest<Result>;

public class SendEmailToUsersCommandHandler : IRequestHandler<SendEmailToUsersCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMessageScheduler _scheduler;

    public SendEmailToUsersCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IMessageScheduler scheduler)
    {
        _db = db;
        _currentUser = currentUser;
        _scheduler = scheduler;
    }

    public async Task<Result> Handle(SendEmailToUsersCommand request, CancellationToken cancellationToken)
    {
        await UserMessageEnqueuer.EnqueueAsync(
            _db, _scheduler, _currentUser.UserId, ScheduledMessageChannel.Email,
            request.UserIds, request.Subject, request.Body, request.SendAtUtc, cancellationToken);

        return Result.Success();
    }
}
