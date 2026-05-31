using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Workouts.Commands;

public record AddWorkoutSetCommand(
    int WorkoutId,
    int WorkoutExerciseId,
    int SetNumber,
    decimal Weight,
    int Reps) : IRequest<Result<IdResponse>>;

public class AddWorkoutSetCommandHandler : IRequestHandler<AddWorkoutSetCommand, Result<IdResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddWorkoutSetCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IdResponse>> Handle(AddWorkoutSetCommand request, CancellationToken cancellationToken)
    {
        var workout = await _db.Workouts
            .Include(w => w.Exercises)
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.ClientId == _currentUser.UserId, cancellationToken);
        if (workout == null) return Result<IdResponse>.NotFound();
        if (!workout.Exercises.Any(we => we.Id == request.WorkoutExerciseId))
            return Result<IdResponse>.Fail(ResultError.Validation, "Exercise does not belong to this workout.");

        var entity = new WorkoutSet
        {
            WorkoutExerciseId = request.WorkoutExerciseId,
            SetNumber = request.SetNumber,
            Weight = request.Weight,
            Reps = request.Reps
        };
        _db.WorkoutSets.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<IdResponse>.Success(new IdResponse(entity.Id));
    }
}
