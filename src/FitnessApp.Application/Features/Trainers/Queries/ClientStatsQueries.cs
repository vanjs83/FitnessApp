using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Progress;
using FitnessApp.Application.DTOs.Stats;
using FitnessApp.Application.Features.Progress;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Trainers.Queries;

// ===== Distinct exercises the client has actually trained =====

public record GetClientTrainedExercisesQuery(string ClientId) : IRequest<Result<IReadOnlyList<TrainedExerciseDto>>>;

public class GetClientTrainedExercisesQueryHandler : IRequestHandler<GetClientTrainedExercisesQuery, Result<IReadOnlyList<TrainedExerciseDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetClientTrainedExercisesQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<TrainedExerciseDto>>> Handle(GetClientTrainedExercisesQuery request, CancellationToken cancellationToken)
    {
        var error = await TrainerGuard.CheckOwnClientAsync(_users, request.ClientId, _currentUser.UserId, cancellationToken);
        if (error != null) return Result<IReadOnlyList<TrainedExerciseDto>>.Fail(error.Value);

        var data = await _db.PerformedSets
            .Where(ps => ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId == request.ClientId)
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

        return Result<IReadOnlyList<TrainedExerciseDto>>.Success(data);
    }
}

// ===== Weight/volume progression of one exercise for the client =====

public record GetClientExerciseProgressQuery(string ClientId, int ExerciseId) : IRequest<Result<IReadOnlyList<ExerciseProgressPointDto>>>;

public class GetClientExerciseProgressQueryHandler : IRequestHandler<GetClientExerciseProgressQuery, Result<IReadOnlyList<ExerciseProgressPointDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetClientExerciseProgressQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<ExerciseProgressPointDto>>> Handle(GetClientExerciseProgressQuery request, CancellationToken cancellationToken)
    {
        var error = await TrainerGuard.CheckOwnClientAsync(_users, request.ClientId, _currentUser.UserId, cancellationToken);
        if (error != null) return Result<IReadOnlyList<ExerciseProgressPointDto>>.Fail(error.Value);

        var sets = await _db.PerformedSets
            .Where(ps => ps.PlannedExercise.ExerciseId == request.ExerciseId
                         && ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId == request.ClientId)
            .Select(ps => new { ps.PerformedAt, ps.ActualReps, ps.ActualWeightKg })
            .ToListAsync(cancellationToken);

        var data = sets
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

        return Result<IReadOnlyList<ExerciseProgressPointDto>>.Success(data);
    }
}

// ===== The client's progress photos (read-only for their trainer) =====

public record GetClientProgressPhotosQuery(string ClientId, int? PlanId = null) : IRequest<Result<IReadOnlyList<ProgressPhotoDto>>>;

public class GetClientProgressPhotosQueryHandler : IRequestHandler<GetClientProgressPhotosQuery, Result<IReadOnlyList<ProgressPhotoDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetClientProgressPhotosQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<ProgressPhotoDto>>> Handle(GetClientProgressPhotosQuery request, CancellationToken cancellationToken)
    {
        var error = await TrainerGuard.CheckOwnClientAsync(_users, request.ClientId, _currentUser.UserId, cancellationToken);
        if (error != null) return Result<IReadOnlyList<ProgressPhotoDto>>.Fail(error.Value);

        var photos = await _db.ProgressPhotos
            .Where(p => p.ClientId == request.ClientId)
            .Where(p => request.PlanId == null || p.PlanId == request.PlanId)
            .OrderByDescending(p => p.TakenOn)
            .ThenByDescending(p => p.CreatedAt)
            .Select(ProgressMapping.ToDto)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProgressPhotoDto>>.Success(photos);
    }
}
