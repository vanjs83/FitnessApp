using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Application.Features.Trainers;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Appointments.Commands;

/// <summary>Trainer books a confirmed session for one of their own clients.</summary>
public record CreateAppointmentCommand(
    string ClientId,
    DateTime StartsAt,
    int DurationMinutes,
    AppointmentType Type,
    string? Location,
    string? Notes) : IRequest<Result<AppointmentDto>>;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result<AppointmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IPushNotificationService _push;

    public CreateAppointmentCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users, IPushNotificationService push)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
        _push = push;
    }

    public async Task<Result<AppointmentDto>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var trainerId = _currentUser.UserId;

        if (AppointmentTime.IsClearlyInPast(request.StartsAt))
            return Result<AppointmentDto>.Fail(ResultError.Validation, "The session time is in the past.");

        var guard = await TrainerGuard.CheckOwnClientAsync(_users, request.ClientId, trainerId, cancellationToken);
        if (guard is not null) return Result<AppointmentDto>.Fail(guard.Value);

        if (await AppointmentConflict.TrainerBusyAsync(_db, trainerId, request.StartsAt, request.DurationMinutes, null, cancellationToken))
            return Result<AppointmentDto>.Conflict("You already have a session booked in that time slot.");

        var appointment = new Appointment
        {
            TrainerId = trainerId,
            ClientId = request.ClientId,
            StartsAt = request.StartsAt,
            DurationMinutes = request.DurationMinutes,
            Type = request.Type,
            Location = request.Location,
            Notes = request.Notes,
            Status = AppointmentStatus.Scheduled
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);

        // Best-effort push to the client; the service never throws (no tokens / Firebase off → no-op).
        var (title, body, data) = AppointmentHelper.Booked(appointment);
        await _push.SendToUserAsync(appointment.ClientId, title, body, data, cancellationToken);

        return Result<AppointmentDto>.Success(
            await AppointmentResolver.ToDtoAsync(appointment, trainerId, _users, cancellationToken));
    }
}
