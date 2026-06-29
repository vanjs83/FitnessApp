using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Appointments.Commands;

// ===== A group member confirms they will attend a group session =====
public record ConfirmGroupAttendanceCommand(int AppointmentId) : IRequest<Result>;

public class ConfirmGroupAttendanceCommandHandler : IRequestHandler<ConfirmGroupAttendanceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ConfirmGroupAttendanceCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ConfirmGroupAttendanceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var appointment = await _db.Appointments
            .Include(a => a.Group!).ThenInclude(g => g.Members)
            .Include(a => a.Attendances)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment is null) return Result.NotFound();
        if (!appointment.IsGroup) return Result.Fail(ResultError.Validation, "This is not a group session.");
        if (appointment.Group is null || appointment.Group.Members.All(m => m.ClientId != userId))
            return Result.Forbidden();
        if (appointment.Status != AppointmentStatus.Scheduled)
            return Result.Fail(ResultError.Validation, "This session is not open for confirmation.");
        if (appointment.StartsAt <= DateTime.UtcNow)
            return Result.Fail(ResultError.Validation, "This session has already started.");

        // Idempotent: confirming again is a no-op.
        if (appointment.Attendances.All(at => at.ClientId != userId))
        {
            _db.GroupAttendances.Add(new GroupAttendance { AppointmentId = appointment.Id, ClientId = userId });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

// ===== A group member withdraws their confirmation =====
public record CancelGroupAttendanceCommand(int AppointmentId) : IRequest<Result>;

public class CancelGroupAttendanceCommandHandler : IRequestHandler<CancelGroupAttendanceCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelGroupAttendanceCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelGroupAttendanceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var appointment = await _db.Appointments
            .Include(a => a.Attendances)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment is null) return Result.NotFound();
        if (!appointment.IsGroup) return Result.Fail(ResultError.Validation, "This is not a group session.");
        // Once the session starts the attendee count is final — no withdrawing after the fact.
        if (appointment.StartsAt <= DateTime.UtcNow)
            return Result.Fail(ResultError.Validation, "This session has already started.");

        // Idempotent: withdrawing when not confirmed is a no-op.
        var attendance = appointment.Attendances.FirstOrDefault(at => at.ClientId == userId);
        if (attendance is not null)
        {
            _db.GroupAttendances.Remove(attendance);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
