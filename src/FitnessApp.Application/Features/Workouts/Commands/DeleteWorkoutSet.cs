using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Workouts.Commands;

public record DeleteWorkoutSetCommand(int SetId) : IRequest<Result>;

public class DeleteWorkoutSetCommandHandler : IRequestHandler<DeleteWorkoutSetCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteWorkoutSetCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteWorkoutSetCommand request, CancellationToken cancellationToken)
    {
        var set = await _db.WorkoutSets
            .Include(s => s.WorkoutExercise).ThenInclude(we => we.Workout)
            .FirstOrDefaultAsync(s => s.Id == request.SetId, cancellationToken);
        if (set == null) return Result.NotFound();
        if (set.WorkoutExercise.Workout.ClientId != _currentUser.UserId) return Result.Forbidden();

        _db.WorkoutSets.Remove(set);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
