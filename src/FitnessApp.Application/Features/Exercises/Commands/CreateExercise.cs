using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Exercises;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Exercises.Commands;

public record CreateExerciseCommand(
    string Name,
    string? Description,
    string? VideoUrl,
    string? MuscleGroup,
    ExerciseType Type) : IRequest<ExerciseDto>;

public class CreateExerciseCommandHandler : IRequestHandler<CreateExerciseCommand, ExerciseDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ExerciseDto> Handle(CreateExerciseCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var entity = new Exercise
        {
            Name = request.Name,
            Description = request.Description,
            VideoUrl = ExerciseMapping.NormalizeVideoUrl(request.VideoUrl),
            MuscleGroup = request.MuscleGroup,
            Type = request.Type,
            CreatedByUserId = userId
        };

        _db.Exercises.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity.ToDto(userId);
    }
}
