using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Queries;

public record GetNutritionPlanByIdQuery(int Id) : IRequest<Result<NutritionPlanDetailDto>>;

public class GetNutritionPlanByIdQueryHandler
    : IRequestHandler<GetNutritionPlanByIdQuery, Result<NutritionPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetNutritionPlanByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<NutritionPlanDetailDto>> Handle(
        GetNutritionPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        var plan = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<NutritionPlanDetailDto>.NotFound();

        if (plan.IsTemplate)
        {
            if (!isTrainer || plan.TrainerId != userId) return Result<NutritionPlanDetailDto>.Forbidden();
            return Result<NutritionPlanDetailDto>.Success(NutritionMapping.MapDetail(plan, "", false));
        }

        if (isTrainer && plan.TrainerId != userId) return Result<NutritionPlanDetailDto>.Forbidden();
        if (!isTrainer && plan.ClientId != userId) return Result<NutritionPlanDetailDto>.Forbidden();

        var client = plan.ClientId != null ? await _users.FindAsync(plan.ClientId, cancellationToken) : null;
        if (!isTrainer && client?.TrainerId != plan.TrainerId) return Result<NutritionPlanDetailDto>.Forbidden();

        var isLocked = !isTrainer && plan.PaymentStatus != PaymentStatus.Approved;
        return Result<NutritionPlanDetailDto>.Success(NutritionMapping.MapDetail(plan, client?.DisplayName ?? "", isLocked));
    }
}
