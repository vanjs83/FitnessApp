using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Queries;

public record GetTrainingPlansForClientQuery(string ClientId)
    : IRequest<Result<IReadOnlyList<TrainingPlanListItemDto>>>;

public class GetTrainingPlansForClientQueryHandler
    : IRequestHandler<GetTrainingPlansForClientQuery, Result<IReadOnlyList<TrainingPlanListItemDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetTrainingPlansForClientQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<TrainingPlanListItemDto>>> Handle(
        GetTrainingPlansForClientQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        if (!isTrainer && request.ClientId != userId)
            return Result<IReadOnlyList<TrainingPlanListItemDto>>.Forbidden();

        var client = await _users.FindAsync(request.ClientId, cancellationToken);
        if (client == null) return Result<IReadOnlyList<TrainingPlanListItemDto>>.NotFound();
        if (isTrainer && client.TrainerId != userId)
            return Result<IReadOnlyList<TrainingPlanListItemDto>>.Forbidden();

        var query = _db.TrainingPlans.Where(p => p.ClientId == request.ClientId && !p.IsTemplate);
        if (!isTrainer)
        {
            if (string.IsNullOrEmpty(client.TrainerId))
                return Result<IReadOnlyList<TrainingPlanListItemDto>>.Success(new List<TrainingPlanListItemDto>());
            query = query.Where(p => p.TrainerId == client.TrainerId);
        }

        var plans = await query
            .OrderByDescending(p => p.StartDate)
            .Select(p => new TrainingPlanListItemDto
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

        return Result<IReadOnlyList<TrainingPlanListItemDto>>.Success(plans);
    }
}
