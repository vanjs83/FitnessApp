using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Queries;

public record GetNutritionPlansForClientQuery(string ClientId)
    : IRequest<Result<IReadOnlyList<NutritionPlanListItemDto>>>;

public class GetNutritionPlansForClientQueryHandler
    : IRequestHandler<GetNutritionPlansForClientQuery, Result<IReadOnlyList<NutritionPlanListItemDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetNutritionPlansForClientQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<NutritionPlanListItemDto>>> Handle(
        GetNutritionPlansForClientQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        if (!isTrainer && request.ClientId != userId)
            return Result<IReadOnlyList<NutritionPlanListItemDto>>.Forbidden();

        var client = await _users.FindAsync(request.ClientId, cancellationToken);
        if (client == null) return Result<IReadOnlyList<NutritionPlanListItemDto>>.NotFound();
        if (isTrainer && client.TrainerId != userId)
            return Result<IReadOnlyList<NutritionPlanListItemDto>>.Forbidden();

        var query = _db.NutritionPlans.Where(p => p.ClientId == request.ClientId && !p.IsTemplate);
        if (!isTrainer)
        {
            if (string.IsNullOrEmpty(client.TrainerId))
                return Result<IReadOnlyList<NutritionPlanListItemDto>>.Success(new List<NutritionPlanListItemDto>());
            query = query.Where(p => p.TrainerId == client.TrainerId);
        }

        var plans = await query
            .OrderByDescending(p => p.StartDate)
            .Select(p => new NutritionPlanListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                ClientId = p.ClientId!,
                ClientName = client.DisplayName,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                DayCount = p.Days.Count,
                Price = p.Price,
                Currency = p.Currency,
                PaymentStatus = p.PaymentStatus
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<NutritionPlanListItemDto>>.Success(plans);
    }
}
