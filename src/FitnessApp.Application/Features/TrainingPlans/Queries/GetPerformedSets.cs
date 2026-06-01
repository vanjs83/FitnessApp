using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Queries;

public record GetPerformedSetsQuery(int PlannedExerciseId) : IRequest<Result<IReadOnlyList<PerformedSetDto>>>;

public class GetPerformedSetsQueryHandler
    : IRequestHandler<GetPerformedSetsQuery, Result<IReadOnlyList<PerformedSetDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPerformedSetsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PerformedSetDto>>> Handle(
        GetPerformedSetsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == request.PlannedExerciseId, cancellationToken);
        if (pe == null) return Result<IReadOnlyList<PerformedSetDto>>.NotFound();

        var plan = pe.TrainingDay.TrainingPlan;
        if (isTrainer && plan.TrainerId != userId) return Result<IReadOnlyList<PerformedSetDto>>.Forbidden();
        if (!isTrainer && plan.ClientId != userId) return Result<IReadOnlyList<PerformedSetDto>>.Forbidden();

        var sets = await _db.PerformedSets
            .Where(ps => ps.PlannedExerciseId == request.PlannedExerciseId)
            .OrderByDescending(ps => ps.PerformedAt)
            .ThenByDescending(ps => ps.SetNumber)
            .Select(ps => new PerformedSetDto
            {
                Id = ps.Id,
                PlannedExerciseId = ps.PlannedExerciseId,
                SetNumber = ps.SetNumber,
                ActualReps = ps.ActualReps,
                ActualWeightKg = ps.ActualWeightKg,
                PerformedAt = ps.PerformedAt,
                Notes = ps.Notes
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PerformedSetDto>>.Success(sets);
    }
}
