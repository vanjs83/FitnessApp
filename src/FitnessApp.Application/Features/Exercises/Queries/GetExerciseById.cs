using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Exercises;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Exercises.Queries;

public record GetExerciseByIdQuery(int Id) : IRequest<Result<ExerciseDto>>;

public class GetExerciseByIdQueryHandler : IRequestHandler<GetExerciseByIdQuery, Result<ExerciseDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetExerciseByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ExerciseDto>> Handle(GetExerciseByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var e = await _db.Exercises.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (e == null) return Result<ExerciseDto>.NotFound();

        if (e.CreatedByUserId != userId)
        {
            var trainerId = await _currentUser.GetTrainerIdAsync(cancellationToken);
            if (e.CreatedByUserId != trainerId) return Result<ExerciseDto>.Forbidden();
        }

        return Result<ExerciseDto>.Success(e.ToDto(userId));
    }
}
