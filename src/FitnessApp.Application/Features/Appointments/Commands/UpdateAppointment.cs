using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Appointments.Commands;

/// <summary>Trainer reschedules / edits one of their sessions (while it is still open).</summary>
public record UpdateAppointmentCommand(
    int Id,
    DateTime StartsAt,
    int DurationMinutes,
    AppointmentType Type,
    string? Location,
    string? Notes) : IRequest<Result<AppointmentDto>>;

public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand, Result<AppointmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public UpdateAppointmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<AppointmentDto>> Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var trainerId = _currentUser.UserId;
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (appointment == null) return Result<AppointmentDto>.NotFound();
        if (appointment.TrainerId != trainerId) return Result<AppointmentDto>.Forbidden();

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
            return Result<AppointmentDto>.Fail(ResultError.Validation, "A closed appointment can no longer be edited.");

        if (await AppointmentConflict.TrainerBusyAsync(_db, trainerId, request.StartsAt, request.DurationMinutes, appointment.Id, cancellationToken))
            return Result<AppointmentDto>.Conflict("You already have a session booked in that time slot.");

        appointment.StartsAt = request.StartsAt;
        appointment.DurationMinutes = request.DurationMinutes;
        appointment.Type = request.Type;
        appointment.Location = request.Location;
        appointment.Notes = request.Notes;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<AppointmentDto>.Success(
            await AppointmentResolver.ToDtoAsync(appointment, trainerId, _users, cancellationToken));
    }
}
