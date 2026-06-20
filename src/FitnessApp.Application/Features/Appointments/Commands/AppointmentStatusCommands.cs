using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Appointments.Commands;

// ===== Trainer confirms a client-requested slot =====
public record ConfirmAppointmentCommand(int Id) : IRequest<Result>;

public class ConfirmAppointmentCommandHandler : IRequestHandler<ConfirmAppointmentCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ConfirmAppointmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ConfirmAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (appointment == null) return Result.NotFound();
        if (appointment.TrainerId != _currentUser.UserId) return Result.Forbidden();
        if (appointment.Status != AppointmentStatus.Requested)
            return Result.Fail(ResultError.Validation, "Only a requested appointment can be confirmed.");

        if (await AppointmentConflict.TrainerBusyAsync(
                _db, appointment.TrainerId, appointment.StartsAt, appointment.DurationMinutes, appointment.Id, cancellationToken))
            return Result.Conflict("You already have a session booked in that time slot.");

        appointment.Status = AppointmentStatus.Scheduled;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ===== Either participant cancels an open appointment =====
public record CancelAppointmentCommand(int Id) : IRequest<Result>;

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelAppointmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (appointment == null) return Result.NotFound();
        if (appointment.TrainerId != userId && appointment.ClientId != userId) return Result.Forbidden();
        if (appointment.Status is not (AppointmentStatus.Requested or AppointmentStatus.Scheduled))
            return Result.Fail(ResultError.Validation, "Only an open appointment can be cancelled.");

        appointment.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ===== Trainer marks the outcome of a scheduled session =====
public record CompleteAppointmentCommand(int Id) : IRequest<Result>;

public class CompleteAppointmentCommandHandler : IRequestHandler<CompleteAppointmentCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CompleteAppointmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
        => await AppointmentOutcome.SetAsync(_db, _currentUser, request.Id, AppointmentStatus.Completed, cancellationToken);
}

public record MarkNoShowCommand(int Id) : IRequest<Result>;

public class MarkNoShowCommandHandler : IRequestHandler<MarkNoShowCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MarkNoShowCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkNoShowCommand request, CancellationToken cancellationToken)
        => await AppointmentOutcome.SetAsync(_db, _currentUser, request.Id, AppointmentStatus.NoShow, cancellationToken);
}

internal static class AppointmentOutcome
{
    public static async Task<Result> SetAsync(
        IAppDbContext db, ICurrentUserService currentUser, int id, AppointmentStatus outcome, CancellationToken cancellationToken)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (appointment == null) return Result.NotFound();
        if (appointment.TrainerId != currentUser.UserId) return Result.Forbidden();
        if (appointment.Status != AppointmentStatus.Scheduled)
            return Result.Fail(ResultError.Validation, "Only a scheduled appointment can be closed.");

        appointment.Status = outcome;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
