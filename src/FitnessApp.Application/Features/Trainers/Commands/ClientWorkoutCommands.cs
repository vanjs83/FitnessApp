using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Workouts;
using FitnessApp.Application.Features.Workouts;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Trainers.Commands;

// ===== Create a workout for a client =====

public record CreateClientWorkoutCommand(
    string ClientId, string Name, DateTime? PerformedAt, int? DurationMinutes, string? Notes)
    : IRequest<Result<WorkoutDetailDto>>;

public class CreateClientWorkoutCommandHandler : IRequestHandler<CreateClientWorkoutCommand, Result<WorkoutDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public CreateClientWorkoutCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<WorkoutDetailDto>> Handle(CreateClientWorkoutCommand request, CancellationToken cancellationToken)
    {
        var error = await TrainerGuard.CheckOwnClientAsync(_users, request.ClientId, _currentUser.UserId, cancellationToken);
        if (error != null) return Result<WorkoutDetailDto>.Fail(error.Value);

        var workout = new Workout
        {
            ClientId = request.ClientId,
            TrainerId = _currentUser.UserId,
            Name = request.Name,
            PerformedAt = request.PerformedAt ?? DateTime.UtcNow,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes
        };
        _db.Workouts.Add(workout);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<WorkoutDetailDto>.Success(WorkoutMapping.MapDetail(workout));
    }
}

// ===== Delete a client workout =====

public record DeleteClientWorkoutCommand(string ClientId, int WorkoutId) : IRequest<Result>;

public class DeleteClientWorkoutCommandHandler : IRequestHandler<DeleteClientWorkoutCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteClientWorkoutCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteClientWorkoutCommand request, CancellationToken cancellationToken)
    {
        var workout = await _db.Workouts
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.ClientId == request.ClientId, cancellationToken);
        if (workout == null) return Result.NotFound();
        if (workout.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.Workouts.Remove(workout);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ===== Add an exercise to a client workout =====

public record AddClientWorkoutExerciseCommand(string ClientId, int WorkoutId, int ExerciseId, int Order)
    : IRequest<Result<IdResponse>>;

public class AddClientWorkoutExerciseCommandHandler : IRequestHandler<AddClientWorkoutExerciseCommand, Result<IdResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddClientWorkoutExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IdResponse>> Handle(AddClientWorkoutExerciseCommand request, CancellationToken cancellationToken)
    {
        var workout = await _db.Workouts
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.ClientId == request.ClientId, cancellationToken);
        if (workout == null) return Result<IdResponse>.NotFound();
        if (workout.TrainerId != _currentUser.UserId) return Result<IdResponse>.Forbidden();

        var exerciseExists = await _db.Exercises.AnyAsync(e => e.Id == request.ExerciseId, cancellationToken);
        if (!exerciseExists) return Result<IdResponse>.Fail(ResultError.Validation, "Exercise not found.");

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

// ===== Remove an exercise from a client workout =====

public record DeleteClientWorkoutExerciseCommand(string ClientId, int WorkoutId, int WorkoutExerciseId) : IRequest<Result>;

public class DeleteClientWorkoutExerciseCommandHandler : IRequestHandler<DeleteClientWorkoutExerciseCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteClientWorkoutExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteClientWorkoutExerciseCommand request, CancellationToken cancellationToken)
    {
        var we = await _db.WorkoutExercises
            .Include(x => x.Workout)
            .FirstOrDefaultAsync(x => x.Id == request.WorkoutExerciseId && x.WorkoutId == request.WorkoutId, cancellationToken);
        if (we == null || we.Workout.ClientId != request.ClientId) return Result.NotFound();
        if (we.Workout.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.WorkoutExercises.Remove(we);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ===== Add a set to a client workout exercise =====

public record AddClientWorkoutSetCommand(
    string ClientId, int WorkoutId, int WorkoutExerciseId, int SetNumber, decimal Weight, int Reps)
    : IRequest<Result<IdResponse>>;

public class AddClientWorkoutSetCommandHandler : IRequestHandler<AddClientWorkoutSetCommand, Result<IdResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddClientWorkoutSetCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IdResponse>> Handle(AddClientWorkoutSetCommand request, CancellationToken cancellationToken)
    {
        var workout = await _db.Workouts
            .Include(w => w.Exercises)
            .FirstOrDefaultAsync(w => w.Id == request.WorkoutId && w.ClientId == request.ClientId, cancellationToken);
        if (workout == null) return Result<IdResponse>.NotFound();
        if (workout.TrainerId != _currentUser.UserId) return Result<IdResponse>.Forbidden();
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

// ===== Delete a set from a client workout =====

public record DeleteClientWorkoutSetCommand(string ClientId, int SetId) : IRequest<Result>;

public class DeleteClientWorkoutSetCommandHandler : IRequestHandler<DeleteClientWorkoutSetCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteClientWorkoutSetCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteClientWorkoutSetCommand request, CancellationToken cancellationToken)
    {
        var set = await _db.WorkoutSets
            .Include(s => s.WorkoutExercise).ThenInclude(we => we.Workout)
            .FirstOrDefaultAsync(s => s.Id == request.SetId, cancellationToken);
        if (set == null) return Result.NotFound();
        if (set.WorkoutExercise.Workout.ClientId != request.ClientId)
            return Result.Fail(ResultError.Validation, "Set does not belong to this client.");
        if (set.WorkoutExercise.Workout.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.WorkoutSets.Remove(set);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
