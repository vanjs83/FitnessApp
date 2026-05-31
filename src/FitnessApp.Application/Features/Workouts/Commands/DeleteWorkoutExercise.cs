using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Workouts.Commands;

public record DeleteWorkoutExerciseCommand(int WorkoutExerciseId) : IRequest<Result>;

public class DeleteWorkoutExerciseCommandHandler : IRequestHandler<DeleteWorkoutExerciseCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteWorkoutExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteWorkoutExerciseCommand request, CancellationToken cancellationToken)
    {
        var we = await _db.WorkoutExercises
            .Include(x => x.Workout)
            .FirstOrDefaultAsync(x => x.Id == request.WorkoutExerciseId, cancellationToken);
        if (we == null) return Result.NotFound();
        if (we.Workout.ClientId != _currentUser.UserId) return Result.Forbidden();

        _db.WorkoutExercises.Remove(we);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
