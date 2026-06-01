using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

public record DeleteTrainingPlanCommand(int Id) : IRequest<Result>;

public class DeleteTrainingPlanCommandHandler : IRequestHandler<DeleteTrainingPlanCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteTrainingPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteTrainingPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result.NotFound();
        if (plan.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.TrainingPlans.Remove(plan);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
