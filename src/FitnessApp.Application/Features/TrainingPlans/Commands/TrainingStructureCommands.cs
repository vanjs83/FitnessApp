using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

// ===== Days =====
public record AddTrainingDayCommand(
    int PlanId,
    DayOfWeek DayOfWeek,
    string Label,
    string? Notes) : IRequest<Result<TrainingDayDto>>;

public class AddTrainingDayCommandHandler : IRequestHandler<AddTrainingDayCommand, Result<TrainingDayDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddTrainingDayCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<TrainingDayDto>> Handle(AddTrainingDayCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan == null) return Result<TrainingDayDto>.NotFound();
        if (plan.TrainerId != _currentUser.UserId) return Result<TrainingDayDto>.Forbidden();

        var day = new TrainingDay
        {
            TrainingPlanId = request.PlanId,
            DayOfWeek = request.DayOfWeek,
            Label = request.Label,
            Notes = request.Notes
        };
        _db.TrainingDays.Add(day);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<TrainingDayDto>.Success(new TrainingDayDto
        {
            Id = day.Id,
            DayOfWeek = day.DayOfWeek,
            Label = day.Label,
            Notes = day.Notes,
            Exercises = new()
        });
    }
}

public record DeleteTrainingDayCommand(int DayId) : IRequest<Result>;

public class DeleteTrainingDayCommandHandler : IRequestHandler<DeleteTrainingDayCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteTrainingDayCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteTrainingDayCommand request, CancellationToken cancellationToken)
    {
        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .FirstOrDefaultAsync(d => d.Id == request.DayId, cancellationToken);
        if (day == null) return Result.NotFound();
        if (day.TrainingPlan.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.TrainingDays.Remove(day);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// ===== Planned exercises =====
public record AddPlannedExerciseCommand(
    int DayId,
    int ExerciseId,
    int Order,
    int TargetSets,
    int TargetReps,
    decimal TargetWeightKg,
    int? TargetDurationSeconds,
    int? RestSeconds,
    string? Notes) : IRequest<Result<PlannedExerciseDto>>;

public class AddPlannedExerciseCommandHandler : IRequestHandler<AddPlannedExerciseCommand, Result<PlannedExerciseDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddPlannedExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PlannedExerciseDto>> Handle(AddPlannedExerciseCommand request, CancellationToken cancellationToken)
    {
        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .FirstOrDefaultAsync(d => d.Id == request.DayId, cancellationToken);
        if (day == null) return Result<PlannedExerciseDto>.NotFound();
        if (day.TrainingPlan.TrainerId != _currentUser.UserId) return Result<PlannedExerciseDto>.Forbidden();

        var exercise = await _db.Exercises.FirstOrDefaultAsync(e => e.Id == request.ExerciseId, cancellationToken);
        if (exercise == null) return Result<PlannedExerciseDto>.Fail(ResultError.Validation, "Exercise not found.");
        if (exercise.CreatedByUserId != _currentUser.UserId)
            return Result<PlannedExerciseDto>.Fail(ResultError.Validation, "You can only use your own exercises.");

        if (request.TargetReps <= 0 && !request.TargetDurationSeconds.HasValue)
            return Result<PlannedExerciseDto>.Fail(ResultError.Validation, "Exercise must have either reps or duration.");

        var pe = new PlannedExercise
        {
            TrainingDayId = request.DayId,
            ExerciseId = request.ExerciseId,
            Order = request.Order,
            TargetSets = request.TargetSets,
            TargetReps = request.TargetReps,
            TargetWeightKg = request.TargetWeightKg,
            TargetDurationSeconds = request.TargetDurationSeconds,
            RestSeconds = request.RestSeconds,
            Notes = request.Notes
        };
        _db.PlannedExercises.Add(pe);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<PlannedExerciseDto>.Success(TrainingMapping.MapPlannedExercise(pe, exercise.Name));
    }
}

public record MovePlannedExerciseCommand(int PlannedExerciseId, string Direction) : IRequest<Result>;

public class MovePlannedExerciseCommandHandler : IRequestHandler<MovePlannedExerciseCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MovePlannedExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MovePlannedExerciseCommand request, CancellationToken cancellationToken)
    {
        if (request.Direction != "up" && request.Direction != "down")
            return Result.Fail(ResultError.Validation, "direction mora biti 'up' ili 'down'.");

        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == request.PlannedExerciseId, cancellationToken);
        if (pe == null) return Result.NotFound();
        if (pe.TrainingDay.TrainingPlan.TrainerId != _currentUser.UserId) return Result.Forbidden();

        var siblings = await _db.PlannedExercises
            .Where(x => x.TrainingDayId == pe.TrainingDayId)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);

        var idx = siblings.FindIndex(x => x.Id == pe.Id);
        var swapIdx = request.Direction == "up" ? idx - 1 : idx + 1;
        if (swapIdx < 0 || swapIdx >= siblings.Count) return Result.Success();

        var other = siblings[swapIdx];
        (pe.Order, other.Order) = (other.Order, pe.Order);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public record DeletePlannedExerciseCommand(int PlannedExerciseId) : IRequest<Result>;

public class DeletePlannedExerciseCommandHandler : IRequestHandler<DeletePlannedExerciseCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeletePlannedExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeletePlannedExerciseCommand request, CancellationToken cancellationToken)
    {
        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == request.PlannedExerciseId, cancellationToken);
        if (pe == null) return Result.NotFound();
        if (pe.TrainingDay.TrainingPlan.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.PlannedExercises.Remove(pe);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
