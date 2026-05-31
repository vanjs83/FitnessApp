using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Exercises;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Exercises.Commands;

public record UpdateExerciseCommand(
    int Id,
    string Name,
    string? Description,
    string? VideoUrl,
    string? MuscleGroup,
    ExerciseType Type) : IRequest<Result<ExerciseDto>>;

public class UpdateExerciseCommandHandler : IRequestHandler<UpdateExerciseCommand, Result<ExerciseDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ExerciseDto>> Handle(UpdateExerciseCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var e = await _db.Exercises.FindAsync(new object?[] { request.Id }, cancellationToken);
        if (e == null) return Result<ExerciseDto>.NotFound();
        if (e.CreatedByUserId != userId) return Result<ExerciseDto>.Forbidden();

        e.Name = request.Name;
        e.Description = request.Description;
        e.VideoUrl = ExerciseMapping.NormalizeVideoUrl(request.VideoUrl);
        e.MuscleGroup = request.MuscleGroup;
        e.Type = request.Type;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<ExerciseDto>.Success(e.ToDto(userId));
    }
}
