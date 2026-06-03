using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Stats;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Queries;

public record GetTrainingPlanWeightProgressionQuery(int Id)
    : IRequest<Result<IReadOnlyList<PlanExerciseProgressionDto>>>;

public class GetTrainingPlanWeightProgressionQueryHandler
    : IRequestHandler<GetTrainingPlanWeightProgressionQuery, Result<IReadOnlyList<PlanExerciseProgressionDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTrainingPlanWeightProgressionQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PlanExerciseProgressionDto>>> Handle(
        GetTrainingPlanWeightProgressionQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        var plan = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.Exercise)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<IReadOnlyList<PlanExerciseProgressionDto>>.NotFound();
        if (isTrainer && plan.TrainerId != userId) return Result<IReadOnlyList<PlanExerciseProgressionDto>>.Forbidden();
        if (!isTrainer && plan.ClientId != userId) return Result<IReadOnlyList<PlanExerciseProgressionDto>>.Forbidden();
        if (!isTrainer && plan.PaymentStatus != PaymentStatus.Approved)
            return Result<IReadOnlyList<PlanExerciseProgressionDto>>.Success(new List<PlanExerciseProgressionDto>());

        var rangeStart = plan.StartDate.Date;
        var rangeEnd = plan.EndDate.Date.AddDays(1);

        var planned = plan.Days
            .SelectMany(d => d.Exercises)
            .GroupBy(pe => pe.ExerciseId)
            .Select(g => new
            {
                ExerciseId = g.Key,
                ExerciseName = g.First().Exercise?.Name ?? "",
                MuscleGroup = g.First().Exercise?.MuscleGroup,
                TargetWeightKg = g.Max(x => x.TargetWeightKg),
                TargetSets = g.Max(x => x.TargetSets),
                TargetReps = g.Max(x => x.TargetReps),
                PlannedExerciseIds = g.Select(x => x.Id).ToList()
            })
            .OrderBy(x => x.ExerciseName)
            .ToList();

        if (planned.Count == 0)
            return Result<IReadOnlyList<PlanExerciseProgressionDto>>.Success(new List<PlanExerciseProgressionDto>());

        var allPlannedIds = planned.SelectMany(p => p.PlannedExerciseIds).ToList();

        var rawSets = await _db.PerformedSets
            .Where(ps => allPlannedIds.Contains(ps.PlannedExerciseId)
                         && ps.PerformedAt >= rangeStart
                         && ps.PerformedAt < rangeEnd)
            .Select(ps => new
            {
                ps.PlannedExerciseId,
                ps.PerformedAt,
                ps.SetNumber,
                ps.ActualReps,
                ps.ActualWeightKg
            })
            .ToListAsync(cancellationToken);

        var plannedIdToExerciseId = planned
            .SelectMany(p => p.PlannedExerciseIds.Select(pid => new { pid, p.ExerciseId }))
            .ToDictionary(x => x.pid, x => x.ExerciseId);

        var pointsByExercise = rawSets
            .Select(s => new
            {
                ExerciseId = plannedIdToExerciseId[s.PlannedExerciseId],
                Point = new ExerciseProgressPointDto
                {
                    Date = s.PerformedAt,
                    SetNumber = s.SetNumber,
                    MaxWeight = s.ActualWeightKg,
                    TotalReps = s.ActualReps,
                    TotalVolume = s.ActualWeightKg * s.ActualReps,
                    SetCount = 1
                }
            })
            .GroupBy(x => x.ExerciseId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Point).OrderBy(p => p.Date).ThenBy(p => p.SetNumber).ToList());

        var result = planned.Select(p => new PlanExerciseProgressionDto
        {
            ExerciseId = p.ExerciseId,
            ExerciseName = p.ExerciseName,
            MuscleGroup = p.MuscleGroup,
            TargetWeightKg = p.TargetWeightKg,
            TargetSets = p.TargetSets,
            TargetReps = p.TargetReps,
            Points = pointsByExercise.TryGetValue(p.ExerciseId, out var pts) ? pts : new List<ExerciseProgressPointDto>()
        }).ToList();

        return Result<IReadOnlyList<PlanExerciseProgressionDto>>.Success(result);
    }
}
