using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Exercises;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Exercises.Queries;

public record GetExercisesQuery : IRequest<IReadOnlyList<ExerciseDto>>;

public class GetExercisesQueryHandler : IRequestHandler<GetExercisesQuery, IReadOnlyList<ExerciseDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetExercisesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ExerciseDto>> Handle(GetExercisesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var trainerId = await _currentUser.GetTrainerIdAsync(cancellationToken);

        return await _db.Exercises
            .Where(e => e.CreatedByUserId == userId
                || (trainerId != null && e.CreatedByUserId == trainerId))
            .OrderBy(e => e.Name)
            .Select(ExerciseMapping.ToDto(userId))
            .ToListAsync(cancellationToken);
    }
}
