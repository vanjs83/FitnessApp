using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Workouts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Workouts.Queries;

public record GetWorkoutByIdQuery(int WorkoutId) : IRequest<Result<WorkoutDetailDto>>;

public class GetWorkoutByIdQueryHandler : IRequestHandler<GetWorkoutByIdQuery, Result<WorkoutDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetWorkoutByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkoutDetailDto>> Handle(GetWorkoutByIdQuery request, CancellationToken cancellationToken)
    {
        var workout = await _db.Workouts
            .Include(w => w.Exercises).ThenInclude(we => we.Exercise)
            .Include(w => w.Exercises).ThenInclude(we => we.Sets)
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.ClientId == _currentUser.UserId, cancellationToken);

        if (workout == null) return Result<WorkoutDetailDto>.NotFound();
        return Result<WorkoutDetailDto>.Success(WorkoutMapping.MapDetail(workout));
    }
}
