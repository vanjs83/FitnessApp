using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Appointments.Commands;

/// <summary>Client asks their assigned trainer for a slot; lands as Requested until the trainer confirms.</summary>
public record RequestAppointmentCommand(
    DateTime StartsAt,
    int DurationMinutes,
    AppointmentType Type,
    string? Location,
    string? Notes) : IRequest<Result<AppointmentDto>>;

public class RequestAppointmentCommandHandler : IRequestHandler<RequestAppointmentCommand, Result<AppointmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly IMessageScheduler _scheduler;

    public RequestAppointmentCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users, IMessageScheduler scheduler)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
        _scheduler = scheduler;
    }

    public async Task<Result<AppointmentDto>> Handle(RequestAppointmentCommand request, CancellationToken cancellationToken)
    {
        var clientId = _currentUser.UserId;

        if (AppointmentTime.IsClearlyInPast(request.StartsAt))
            return Result<AppointmentDto>.Fail(ResultError.Validation, "The session time is in the past.");

        var trainerId = await _currentUser.GetTrainerIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(trainerId))
            return Result<AppointmentDto>.Fail(ResultError.Validation, "You need a trainer assigned to request a session.");

        if (await AppointmentConflict.TrainerBusyAsync(_db, trainerId, request.StartsAt, request.DurationMinutes, null, cancellationToken))
            return Result<AppointmentDto>.Conflict("Your trainer already has a session booked in that time slot.");

        var appointment = new Appointment
        {
            TrainerId = trainerId,
            ClientId = clientId,
            StartsAt = request.StartsAt,
            DurationMinutes = request.DurationMinutes,
            Type = request.Type,
            Location = request.Location,
            Notes = request.Notes,
            Status = AppointmentStatus.Requested
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);

        // Best-effort push to the trainer that a client is requesting a slot.
        var client = await _users.FindAsync(clientId, cancellationToken);
        var (title, body, data) = AppointmentHelper.Requested(appointment, client?.DisplayName ?? "Klijent");
        _scheduler.Schedule<IPushNotificationService>(p => p.SendToUserAsync(trainerId, title, body, data));

        return Result<AppointmentDto>.Success(
            await AppointmentResolver.ToDtoAsync(appointment, clientId, _users, cancellationToken));
    }
}
