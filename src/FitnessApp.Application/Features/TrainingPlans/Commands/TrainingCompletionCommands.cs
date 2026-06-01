using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

/// <summary>Result of toggling a completion; serializes as { isCompletedToday: bool }.</summary>
public record CompletionToggleResponse(bool IsCompletedToday);

// ===== Toggle all exercises of a day for today =====
public record ToggleDayCompletionCommand(int DayId) : IRequest<Result<CompletionToggleResponse>>;

public class ToggleDayCompletionCommandHandler : IRequestHandler<ToggleDayCompletionCommand, Result<CompletionToggleResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleDayCompletionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CompletionToggleResponse>> Handle(ToggleDayCompletionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var day = await _db.TrainingDays
            .Include(d => d.TrainingPlan)
            .Include(d => d.Exercises).ThenInclude(pe => pe.Completions)
            .FirstOrDefaultAsync(d => d.Id == request.DayId, cancellationToken);
        if (day == null) return Result<CompletionToggleResponse>.NotFound();
        if (day.TrainingPlan.ClientId != userId) return Result<CompletionToggleResponse>.Forbidden();
        if (day.TrainingPlan.PaymentStatus != PaymentStatus.Approved)
            return Result<CompletionToggleResponse>.Fail(ResultError.Validation, "Plan is not approved.");
        if (!day.Exercises.Any())
            return Result<CompletionToggleResponse>.Fail(ResultError.Validation, "The day has no exercises to mark.");

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        bool IsCompletedToday(PlannedExercise pe) =>
            pe.Completions.Any(c => c.ClientId == userId && c.CompletedAt >= today && c.CompletedAt < tomorrow);

        var allCompleted = day.Exercises.All(IsCompletedToday);

        if (allCompleted)
        {
            var toRemove = day.Exercises
                .SelectMany(pe => pe.Completions
                    .Where(c => c.ClientId == userId && c.CompletedAt >= today && c.CompletedAt < tomorrow))
                .ToList();
            _db.PlannedExerciseCompletions.RemoveRange(toRemove);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<CompletionToggleResponse>.Success(new CompletionToggleResponse(false));
        }

        foreach (var pe in day.Exercises.Where(p => !IsCompletedToday(p)))
        {
            _db.PlannedExerciseCompletions.Add(new PlannedExerciseCompletion
            {
                PlannedExerciseId = pe.Id,
                ClientId = userId,
                CompletedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
        return Result<CompletionToggleResponse>.Success(new CompletionToggleResponse(true));
    }
}

// ===== Toggle a single planned exercise for today =====
public record ToggleExerciseTodayCompletionCommand(int PlannedExerciseId) : IRequest<Result<CompletionToggleResponse>>;

public class ToggleExerciseTodayCompletionCommandHandler
    : IRequestHandler<ToggleExerciseTodayCompletionCommand, Result<CompletionToggleResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleExerciseTodayCompletionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CompletionToggleResponse>> Handle(ToggleExerciseTodayCompletionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == request.PlannedExerciseId, cancellationToken);
        if (pe == null) return Result<CompletionToggleResponse>.NotFound();
        if (pe.TrainingDay.TrainingPlan.ClientId != userId) return Result<CompletionToggleResponse>.Forbidden();
        if (pe.TrainingDay.TrainingPlan.PaymentStatus != PaymentStatus.Approved)
            return Result<CompletionToggleResponse>.Fail(ResultError.Validation, "Plan is not approved.");

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var existing = await _db.PlannedExerciseCompletions
            .FirstOrDefaultAsync(c => c.PlannedExerciseId == request.PlannedExerciseId
                                       && c.ClientId == userId
                                       && c.CompletedAt >= today
                                       && c.CompletedAt < tomorrow, cancellationToken);

        if (existing != null)
        {
            _db.PlannedExerciseCompletions.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<CompletionToggleResponse>.Success(new CompletionToggleResponse(false));
        }

        _db.PlannedExerciseCompletions.Add(new PlannedExerciseCompletion
        {
            PlannedExerciseId = request.PlannedExerciseId,
            ClientId = userId,
            CompletedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Result<CompletionToggleResponse>.Success(new CompletionToggleResponse(true));
    }
}
