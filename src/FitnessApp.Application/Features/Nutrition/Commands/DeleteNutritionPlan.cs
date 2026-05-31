using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Commands;

public record DeleteNutritionPlanCommand(int Id) : IRequest<Result>;

public class DeleteNutritionPlanCommandHandler : IRequestHandler<DeleteNutritionPlanCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteNutritionPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteNutritionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result.NotFound();
        if (plan.TrainerId != _currentUser.UserId) return Result.Forbidden();

        _db.NutritionPlans.Remove(plan);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
