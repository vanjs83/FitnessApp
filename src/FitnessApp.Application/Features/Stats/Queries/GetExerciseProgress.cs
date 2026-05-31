using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Stats;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Stats.Queries;

public record GetExerciseProgressQuery(int ExerciseId) : IRequest<IReadOnlyList<ExerciseProgressPointDto>>;

public class GetExerciseProgressQueryHandler
    : IRequestHandler<GetExerciseProgressQuery, IReadOnlyList<ExerciseProgressPointDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetExerciseProgressQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ExerciseProgressPointDto>> Handle(
        GetExerciseProgressQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var sets = await _db.PerformedSets
            .Where(ps => ps.PlannedExercise.ExerciseId == request.ExerciseId
                         && ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId == userId)
            .Select(ps => new
            {
                ps.PerformedAt,
                ps.ActualReps,
                ps.ActualWeightKg
            })
            .ToListAsync(cancellationToken);

        return sets
            .GroupBy(ps => ps.PerformedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ExerciseProgressPointDto
            {
                Date = g.Key,
                MaxWeight = g.Max(s => s.ActualWeightKg),
                TotalReps = g.Sum(s => s.ActualReps),
                TotalVolume = g.Sum(s => s.ActualWeightKg * s.ActualReps),
                SetCount = g.Count()
            })
            .ToList();
    }
}
