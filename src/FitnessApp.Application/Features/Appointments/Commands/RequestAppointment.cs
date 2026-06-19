using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
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

    public RequestAppointmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<AppointmentDto>> Handle(RequestAppointmentCommand request, CancellationToken cancellationToken)
    {
        var clientId = _currentUser.UserId;

        var trainerId = await _currentUser.GetTrainerIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(trainerId))
            return Result<AppointmentDto>.Fail(ResultError.Validation, "You need a trainer assigned to request a session.");

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

        return Result<AppointmentDto>.Success(
            await AppointmentResolver.ToDtoAsync(appointment, clientId, _users, cancellationToken));
    }
}
