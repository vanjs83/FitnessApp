using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Workouts;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Workouts.Commands;

public record CreateWorkoutCommand(
    string Name,
    DateTime? PerformedAt,
    int? DurationMinutes,
    string? Notes) : IRequest<Result<WorkoutDetailDto>>;

public class CreateWorkoutCommandHandler : IRequestHandler<CreateWorkoutCommand, Result<WorkoutDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateWorkoutCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkoutDetailDto>> Handle(CreateWorkoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<WorkoutDetailDto>.Fail(ResultError.Validation, "Name is required.");

        var trainerId = await _currentUser.GetTrainerIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(trainerId))
            return Result<WorkoutDetailDto>.Fail(ResultError.Validation, "You need a trainer assigned to log workouts.");

        var workout = new Workout
        {
            ClientId = _currentUser.UserId,
            TrainerId = trainerId,
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
