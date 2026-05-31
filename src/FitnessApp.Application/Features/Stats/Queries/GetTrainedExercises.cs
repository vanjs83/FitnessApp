using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Stats;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Stats.Queries;

public record GetTrainedExercisesQuery : IRequest<IReadOnlyList<TrainedExerciseDto>>;

public class GetTrainedExercisesQueryHandler
    : IRequestHandler<GetTrainedExercisesQuery, IReadOnlyList<TrainedExerciseDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTrainedExercisesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TrainedExerciseDto>> Handle(
        GetTrainedExercisesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        return await _db.PerformedSets
            .Where(ps => ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId == userId)
            .Select(ps => ps.PlannedExercise.Exercise)
            .Distinct()
            .OrderBy(e => e.Name)
            .Select(e => new TrainedExerciseDto
            {
                Id = e.Id,
                Name = e.Name,
                MuscleGroup = e.MuscleGroup,
                Type = e.Type.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
