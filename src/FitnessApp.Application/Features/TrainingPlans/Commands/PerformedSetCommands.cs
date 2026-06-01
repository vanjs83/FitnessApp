using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

// ===== Client logs a performed set =====
public record LogPerformedSetCommand(
    int PlannedExerciseId,
    int SetNumber,
    int ActualReps,
    decimal ActualWeightKg,
    string? Notes) : IRequest<Result<PerformedSetDto>>;

public class LogPerformedSetCommandHandler : IRequestHandler<LogPerformedSetCommand, Result<PerformedSetDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public LogPerformedSetCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PerformedSetDto>> Handle(LogPerformedSetCommand request, CancellationToken cancellationToken)
    {
        var pe = await _db.PlannedExercises
            .Include(x => x.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(x => x.Id == request.PlannedExerciseId, cancellationToken);
        if (pe == null) return Result<PerformedSetDto>.NotFound();
        if (pe.TrainingDay.TrainingPlan.ClientId != _currentUser.UserId) return Result<PerformedSetDto>.Forbidden();
        if (pe.TrainingDay.TrainingPlan.PaymentStatus != PaymentStatus.Approved)
            return Result<PerformedSetDto>.Fail(ResultError.Validation, "Plan is not approved.");

        var entity = new PerformedSet
        {
            PlannedExerciseId = request.PlannedExerciseId,
            SetNumber = request.SetNumber,
            ActualReps = request.ActualReps,
            ActualWeightKg = request.ActualWeightKg,
            PerformedAt = DateTime.UtcNow,
            Notes = request.Notes
        };
        _db.PerformedSets.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<PerformedSetDto>.Success(TrainingMapping.MapPerformedSet(entity));
    }
}

// ===== Client deletes a performed set =====
public record DeletePerformedSetCommand(int Id) : IRequest<Result>;

public class DeletePerformedSetCommandHandler : IRequestHandler<DeletePerformedSetCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeletePerformedSetCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeletePerformedSetCommand request, CancellationToken cancellationToken)
    {
        var ps = await _db.PerformedSets
            .Include(s => s.PlannedExercise).ThenInclude(pe => pe.TrainingDay).ThenInclude(d => d.TrainingPlan)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (ps == null) return Result.NotFound();
        if (ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId != _currentUser.UserId) return Result.Forbidden();
        if (ps.PlannedExercise.TrainingDay.TrainingPlan.PaymentStatus != PaymentStatus.Approved)
            return Result.Fail(ResultError.Validation, "Plan is not approved.");

        _db.PerformedSets.Remove(ps);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
