using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Application.Features.Appointments;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Groups.Commands;

/// <summary>
/// Trainer books one confirmed session for a whole group and pushes it to every member.
/// A group session is just an <see cref="Appointment"/> with GroupId set (and no ClientId).
/// </summary>
public record CreateGroupSessionCommand(
    int GroupId,
    DateTime StartsAt,
    int DurationMinutes,
    AppointmentType Type,
    string? Location,
    string? Notes) : IRequest<Result<AppointmentDto>>;

public class CreateGroupSessionCommandHandler : IRequestHandler<CreateGroupSessionCommand, Result<AppointmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPushNotificationService _push;
    private readonly IUserDirectory _users;

    public CreateGroupSessionCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, IPushNotificationService push, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _push = push;
        _users = users;
    }

    public async Task<Result<AppointmentDto>> Handle(CreateGroupSessionCommand request, CancellationToken cancellationToken)
    {
        var trainerId = _currentUser.UserId;

        if (AppointmentTime.IsClearlyInPast(request.StartsAt))
            return Result<AppointmentDto>.Fail(ResultError.Validation, "The session time is in the past.");

        var (group, error) = await GroupGuard.LoadOwnedAsync(_db, request.GroupId, trainerId, cancellationToken);
        if (error is not null) return Result<AppointmentDto>.Fail(error.Value);

        // One conflict check now covers everything — group sessions are appointments too.
        if (await AppointmentConflict.TrainerBusyAsync(_db, trainerId, request.StartsAt, request.DurationMinutes, null, cancellationToken))
            return Result<AppointmentDto>.Conflict("You already have a session booked in that time slot.");

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
