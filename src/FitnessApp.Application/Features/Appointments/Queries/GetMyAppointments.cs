using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Appointments;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Appointments.Queries;

/// <summary>
/// Appointments the caller takes part in (as trainer or as client), optionally bounded
/// to a date range so the calendar pulls one window at a time. Defaults to upcoming.
/// </summary>
public record GetMyAppointmentsQuery(DateTime? From = null, DateTime? To = null)
    : IRequest<IReadOnlyList<AppointmentDto>>;

public class GetMyAppointmentsQueryHandler
    : IRequestHandler<GetMyAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetMyAppointmentsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<IReadOnlyList<AppointmentDto>> Handle(GetMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var from = request.From ?? DateTime.UtcNow.Date;
        var to = request.To ?? from.AddMonths(3);

        var appointments = await _db.Appointments
            .Include(a => a.Group!).ThenInclude(g => g.Members)
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.StartsAt >= from && a.StartsAt < to)
            // Caller is involved as trainer, as the individual client, or as a member of the group.
            .Where(a => a.TrainerId == userId || a.ClientId == userId
                        || (a.GroupId != null && a.Group!.Members.Any(m => m.ClientId == userId)))
            .OrderBy(a => a.StartsAt)
            .ToListAsync(cancellationToken);

        // Only individual sessions need a counterpart name; group sessions use the group name.
        var counterpartIds = appointments
            .Where(a => !a.IsGroup)
            .Select(a => a.TrainerId == userId ? a.ClientId : a.TrainerId)
            .Where(id => id != null).Select(id => id!)
            .Distinct();
        var names = await _users.GetDisplayNamesAsync(counterpartIds, cancellationToken);

        return appointments.Select(a => AppointmentMapping.ToDto(a, userId, names)).ToList();
    }
}
