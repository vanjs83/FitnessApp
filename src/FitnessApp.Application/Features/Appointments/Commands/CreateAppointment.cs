using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Application.Features.Groups;
using FitnessApp.Application.Features.Trainers;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Appointments.Commands;

/// <summary>
/// Trainer books a confirmed session: individual (<see cref="ClientId"/>) or for a whole group
/// (<see cref="GroupId"/>). A group booking pushes to every member.
/// </summary>
public record CreateAppointmentCommand(
    string? ClientId,
    int? GroupId,
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

        // One conflict check covers both kinds — they're all the trainer's appointments.
        if (await AppointmentConflict.TrainerBusyAsync(_db, trainerId, request.StartsAt, request.DurationMinutes, null, cancellationToken))
            return Result<AppointmentDto>.Conflict("You already have a session booked in that time slot.");

        return request.GroupId is int groupId
            ? await CreateGroupAsync(trainerId, groupId, request, cancellationToken)
            : await CreateIndividualAsync(trainerId, request, cancellationToken);
    }

    private async Task<Result<AppointmentDto>> CreateIndividualAsync(
        string trainerId, CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
            return Result<AppointmentDto>.Fail(ResultError.Validation, "A client or a group is required.");

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

        // Best-effort push to the client; the service never throws (no tokens / Firebase off → no-op).
        var (title, body, data) = AppointmentHelper.Booked(appointment);
        await _push.SendToUserAsync(appointment.ClientId!, title, body, data, cancellationToken);

        return Result<AppointmentDto>.Success(
            await AppointmentResolver.ToDtoAsync(appointment, trainerId, _users, cancellationToken));
    }

    private async Task<Result<AppointmentDto>> CreateGroupAsync(
        string trainerId, int groupId, CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var (group, error) = await GroupGuard.LoadOwnedAsync(_db, groupId, trainerId, cancellationToken);
        if (error is not null) return Result<AppointmentDto>.Fail(error.Value);

        var appointment = new Appointment
        {
            TrainerId = trainerId,
            GroupId = group!.Id,
            Group = group,
            StartsAt = request.StartsAt,
            DurationMinutes = request.DurationMinutes,
            Type = request.Type,
            Location = request.Location,
            Notes = request.Notes,
            Status = AppointmentStatus.Scheduled
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);

        // Best-effort push fan-out to each group member, listing the whole roster in the body.
        var memberIds = group.Members.Select(m => m.ClientId).ToList();
        var names = await _users.GetDisplayNamesAsync(memberIds, cancellationToken);
        var memberNames = memberIds.Select(id => names.TryGetValue(id, out var n) ? n : "").ToList();
        var (title, body, data) = AppointmentHelper.GroupBooked(appointment, group.Name, memberNames);
        foreach (var member in group.Members)
            await _push.SendToUserAsync(member.ClientId, title, body, data, cancellationToken);

        return Result<AppointmentDto>.Success(
            await AppointmentResolver.ToDtoAsync(appointment, trainerId, _users, cancellationToken));
    }
}
