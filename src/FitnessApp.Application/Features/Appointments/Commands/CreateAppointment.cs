using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Application.Features.Trainers;
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

    public CreateAppointmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<AppointmentDto>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var trainerId = _currentUser.UserId;

        var guard = await TrainerGuard.CheckOwnClientAsync(_users, request.ClientId, trainerId, cancellationToken);
        if (guard is not null) return Result<AppointmentDto>.Fail(guard.Value);

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

        return Result<AppointmentDto>.Success(
            await AppointmentResolver.ToDtoAsync(appointment, trainerId, _users, cancellationToken));
    }
}
