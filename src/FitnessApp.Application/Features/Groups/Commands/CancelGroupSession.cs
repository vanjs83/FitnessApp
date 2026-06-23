using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Features.Appointments;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Groups.Commands;

/// <summary>Trainer cancels a group session; members are notified it's off.</summary>
public record CancelGroupSessionCommand(int SessionId) : IRequest<Result>;

public class CancelGroupSessionCommandHandler : IRequestHandler<CancelGroupSessionCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPushNotificationService _push;
    private readonly IUserDirectory _users;

    public CancelGroupSessionCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, IPushNotificationService push, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _push = push;
        _users = users;
    }

    public async Task<Result> Handle(CancelGroupSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.Appointments
            .Include(a => a.Group!).ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(a => a.Id == request.SessionId && a.GroupId != null, cancellationToken);

        if (session is null) return Result.NotFound();
        if (session.TrainerId != _currentUser.UserId) return Result.Forbidden();
        if (session.Status != AppointmentStatus.Scheduled)
            return Result.Fail(ResultError.Validation, "Only a scheduled session can be cancelled.");

        // Snapshot members before removal so we can still notify them.
        var members = (session.Group?.Members ?? Enumerable.Empty<TrainingGroupMember>()).ToList();

        // Cancelling removes the session entirely.
        _db.Appointments.Remove(session);
        await _db.SaveChangesAsync(cancellationToken);

        // Best-effort push fan-out so members know the session is off, listing the whole roster.
        var memberIds = members.Select(m => m.ClientId).ToList();
        var names = await _users.GetDisplayNamesAsync(memberIds, cancellationToken);
        var memberNames = memberIds.Select(id => names.TryGetValue(id, out var n) ? n : "").ToList();
        var (title, body, data) = AppointmentHelper.GroupCancelled(session, session.Group?.Name ?? "", memberNames);
        foreach (var member in members)
            await _push.SendToUserAsync(member.ClientId, title, body, data, cancellationToken);

        return Result.Success();
    }
}
