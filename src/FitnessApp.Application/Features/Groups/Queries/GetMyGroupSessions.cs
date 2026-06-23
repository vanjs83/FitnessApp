using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Groups.Queries;

/// <summary>
/// Group sessions for the calendar. A trainer sees sessions they run; a client sees sessions of
/// the groups they belong to. Optionally bounded to a date range; cancelled sessions are excluded.
/// </summary>
public record GetMyGroupSessionsQuery(DateTime? From = null, DateTime? To = null)
    : IRequest<IReadOnlyList<GroupSessionDto>>;

public class GetMyGroupSessionsQueryHandler : IRequestHandler<GetMyGroupSessionsQuery, IReadOnlyList<GroupSessionDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyGroupSessionsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<GroupSessionDto>> Handle(GetMyGroupSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var from = request.From ?? DateTime.UtcNow.Date;
        var to = request.To ?? from.AddMonths(3);

        var sessions = await _db.GroupSessions
            .Include(s => s.Group!).ThenInclude(g => g.Members)
            .Where(s => s.Status != AppointmentStatus.Cancelled && s.StartsAt >= from && s.StartsAt < to)
            // The caller is involved if they run the session or are a member of its group.
            .Where(s => s.TrainerId == userId || s.Group!.Members.Any(m => m.ClientId == userId))
            .OrderBy(s => s.StartsAt)
            .ToListAsync(cancellationToken);

        return sessions.Select(s => GroupSessionMapping.ToDto(s, s.Group!)).ToList();
    }
}
