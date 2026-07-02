using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.ScheduledMessages.Commands;

public record CancelScheduledMessageCommand(int Id) : IRequest<Result>;

public class CancelScheduledMessageCommandHandler : IRequestHandler<CancelScheduledMessageCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelScheduledMessageCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelScheduledMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _db.ScheduledMessages.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (message is null) return Result.NotFound();

        // The sender owns it; an admin may cancel anyone's.
        if (message.SenderId != _currentUser.UserId && !_currentUser.IsInRole(Roles.SuperAdmin))
            return Result.Forbidden();

        if (message.Status != ScheduledMessageStatus.Pending)
            return Result.Fail(ResultError.Validation, "Only a pending message can be cancelled.");

        message.Status = ScheduledMessageStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
