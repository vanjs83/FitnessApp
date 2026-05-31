using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Exercises.Commands;

public record DeleteExerciseCommand(int Id) : IRequest<Result>;

public class DeleteExerciseCommandHandler : IRequestHandler<DeleteExerciseCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteExerciseCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteExerciseCommand request, CancellationToken cancellationToken)
    {
        var e = await _db.Exercises.FindAsync(new object?[] { request.Id }, cancellationToken);
        if (e == null) return Result.NotFound();
        if (e.CreatedByUserId != _currentUser.UserId) return Result.Forbidden();

        var usedInPlan = await _db.PlannedExercises.AnyAsync(pe => pe.ExerciseId == request.Id, cancellationToken);
        if (usedInPlan)
            return Result.Conflict("Exercise is in use in plans — remove it from them first.");

        _db.Exercises.Remove(e);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
