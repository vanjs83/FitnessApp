using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Workouts.Commands;

public record AddWorkoutExerciseCommand(int WorkoutId, int ExerciseId, int Order) : IRequest<Result<IdResponse>>;

public class AddWorkoutExerciseCommandHandler : IRequestHandler<AddWorkoutExerciseCommand, Result<IdResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddWorkoutExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IdResponse>> Handle(AddWorkoutExerciseCommand request, CancellationToken cancellationToken)
    {
        var workout = await _db.Workouts
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.ClientId == _currentUser.UserId, cancellationToken);
        if (workout == null) return Result<IdResponse>.NotFound();

        var exists = await _db.Exercises.AnyAsync(e => e.Id == request.ExerciseId, cancellationToken);
        if (!exists) return Result<IdResponse>.Fail(ResultError.Validation, "Exercise not found.");

        var entity = new WorkoutExercise
        {
            WorkoutId = request.WorkoutId,
            ExerciseId = request.ExerciseId,
            Order = request.Order
        };
        _db.WorkoutExercises.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<IdResponse>.Success(new IdResponse(entity.Id));
    }
}
