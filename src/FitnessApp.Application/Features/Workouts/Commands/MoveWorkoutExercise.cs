using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Workouts.Commands;

public record MoveWorkoutExerciseCommand(int WorkoutExerciseId, string Direction) : IRequest<Result>;

public class MoveWorkoutExerciseCommandHandler : IRequestHandler<MoveWorkoutExerciseCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MoveWorkoutExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MoveWorkoutExerciseCommand request, CancellationToken cancellationToken)
    {
        if (request.Direction != "up" && request.Direction != "down")
            return Result.Fail(ResultError.Validation, "Direction must be 'up' or 'down'.");

        var we = await _db.WorkoutExercises
            .Include(x => x.Workout)
            .FirstOrDefaultAsync(x => x.Id == request.WorkoutExerciseId, cancellationToken);
        if (we == null) return Result.NotFound();
        if (we.Workout.ClientId != _currentUser.UserId) return Result.Forbidden();

        var siblings = await _db.WorkoutExercises
            .Where(x => x.WorkoutId == we.WorkoutId)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);

        var idx = siblings.FindIndex(x => x.Id == we.Id);
        var swapWith = request.Direction == "up" ? idx - 1 : idx + 1;
        if (swapWith < 0 || swapWith >= siblings.Count) return Result.Success();

        (we.Order, siblings[swapWith].Order) = (siblings[swapWith].Order, we.Order);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
