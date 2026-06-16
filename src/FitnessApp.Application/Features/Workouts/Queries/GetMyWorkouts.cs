using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Workouts;
using MediatR;

namespace FitnessApp.Application.Features.Workouts.Queries;

public record GetMyWorkoutsQuery(int Page = 1) : IRequest<PagedResult<WorkoutListItemDto>>;

public class GetMyWorkoutsQueryHandler : IRequestHandler<GetMyWorkoutsQuery, PagedResult<WorkoutListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyWorkoutsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<WorkoutListItemDto>> Handle(GetMyWorkoutsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        return await _db.Workouts
            .Where(w => w.ClientId == userId)
            .OrderByDescending(w => w.PerformedAt)
            .Select(w => new WorkoutListItemDto
            {
                Id = w.Id,
                Name = w.Name,
                PerformedAt = w.PerformedAt,
                DurationMinutes = w.DurationMinutes,
                Notes = w.Notes,
                ExerciseCount = w.Exercises.Count
            })
            .ToPagedResultAsync(request.Page, PaginationExtensions.DefaultPageSize, cancellationToken);
    }
}
