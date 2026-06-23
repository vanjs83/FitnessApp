using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Application.Features.Appointments;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Groups.Commands;

/// <summary>Trainer books one confirmed session for a whole group and pushes it to every member.</summary>
public record CreateGroupSessionCommand(
    int GroupId,
    DateTime StartsAt,
    int DurationMinutes,
    AppointmentType Type,
    string? Location,
    string? Notes) : IRequest<Result<GroupSessionDto>>;

public class CreateGroupSessionCommandHandler : IRequestHandler<CreateGroupSessionCommand, Result<GroupSessionDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPushNotificationService _push;

    public CreateGroupSessionCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IPushNotificationService push)
    {
        _db = db;
        _currentUser = currentUser;
        _push = push;
    }

    public async Task<Result<GroupSessionDto>> Handle(CreateGroupSessionCommand request, CancellationToken cancellationToken)
    {
        var trainerId = _currentUser.UserId;

        if (AppointmentTime.IsClearlyInPast(request.StartsAt))
            return Result<GroupSessionDto>.Fail(ResultError.Validation, "The session time is in the past.");

        var (group, error) = await GroupGuard.LoadOwnedAsync(_db, request.GroupId, trainerId, cancellationToken);
        if (error is not null) return Result<GroupSessionDto>.Fail(error.Value);

        // Trainer must be free across both individual appointments and other group sessions.
        if (await AppointmentConflict.TrainerBusyAsync(_db, trainerId, request.StartsAt, request.DurationMinutes, null, cancellationToken)
            || await GroupSessionConflict.TrainerBusyAsync(_db, trainerId, request.StartsAt, request.DurationMinutes, null, cancellationToken))
            return Result<GroupSessionDto>.Conflict("You already have a session booked in that time slot.");

        var session = new GroupSession
        {
            TrainerId = trainerId,
            GroupId = group!.Id,
            StartsAt = request.StartsAt,
            DurationMinutes = request.DurationMinutes,
            Type = request.Type,
            Location = request.Location,
            Notes = request.Notes,
            Status = AppointmentStatus.Scheduled
        };

        _db.GroupSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        // Best-effort push fan-out to each group member (service never throws).
        var (title, body, data) = AppointmentHelper.GroupBooked(session, group.Name);
        foreach (var member in group.Members)
            await _push.SendToUserAsync(member.ClientId, title, body, data, cancellationToken);

        return Result<GroupSessionDto>.Success(GroupSessionMapping.ToDto(session, group));
    }
}
