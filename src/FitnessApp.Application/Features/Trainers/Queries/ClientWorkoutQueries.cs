using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Workouts;
using FitnessApp.Application.Features.Workouts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Trainers.Queries;

// ===== List of a client's workouts (created by this trainer) =====

public record GetClientWorkoutsQuery(string ClientId, int Page = 1) : IRequest<Result<PagedResult<WorkoutListItemDto>>>;

public class GetClientWorkoutsQueryHandler : IRequestHandler<GetClientWorkoutsQuery, Result<PagedResult<WorkoutListItemDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetClientWorkoutsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<PagedResult<WorkoutListItemDto>>> Handle(GetClientWorkoutsQuery request, CancellationToken cancellationToken)
    {
        var error = await TrainerGuard.CheckOwnClientAsync(_users, request.ClientId, _currentUser.UserId, cancellationToken);
        if (error != null) return Result<PagedResult<WorkoutListItemDto>>.Fail(error.Value);

        var data = await _db.Workouts
            .Where(w => w.ClientId == request.ClientId && w.TrainerId == _currentUser.UserId)
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

        return Result<PagedResult<WorkoutListItemDto>>.Success(data);
    }
}

// ===== One client workout in detail =====

public record GetClientWorkoutByIdQuery(string ClientId, int WorkoutId) : IRequest<Result<WorkoutDetailDto>>;

public class GetClientWorkoutByIdQueryHandler : IRequestHandler<GetClientWorkoutByIdQuery, Result<WorkoutDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetClientWorkoutByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkoutDetailDto>> Handle(GetClientWorkoutByIdQuery request, CancellationToken cancellationToken)
    {
        var workout = await _db.Workouts
            .Include(w => w.Exercises).ThenInclude(we => we.Exercise)
            .Include(w => w.Exercises).ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.ClientId == request.ClientId, cancellationToken);
        if (workout == null) return Result<WorkoutDetailDto>.NotFound();
        if (workout.TrainerId != _currentUser.UserId) return Result<WorkoutDetailDto>.Forbidden();

        return Result<WorkoutDetailDto>.Success(WorkoutMapping.MapDetail(workout));
    }
}
