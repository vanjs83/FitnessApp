using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Queries;

public record GetMyTrainingPlansQuery : IRequest<IReadOnlyList<TrainingPlanListItemDto>>;

public class GetMyTrainingPlansQueryHandler
    : IRequestHandler<GetMyTrainingPlansQuery, IReadOnlyList<TrainingPlanListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetMyTrainingPlansQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<IReadOnlyList<TrainingPlanListItemDto>> Handle(
        GetMyTrainingPlansQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var plans = await _db.TrainingPlans
            .Where(p => p.TrainerId == userId && !p.IsTemplate)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ClientId,
                p.StartDate,
                p.EndDate,
                DayCount = p.Days.Count,
                p.Price,
                p.Currency,
                p.PaymentStatus
            })
            .ToListAsync(cancellationToken);

        var names = await _users.GetDisplayNamesAsync(
            plans.Where(p => p.ClientId != null).Select(p => p.ClientId!), cancellationToken);

        return plans.Select(p => new TrainingPlanListItemDto
        {
            Id = p.Id,
            Name = p.Name,
            ClientId = p.ClientId ?? "",
            ClientName = p.ClientId != null && names.TryGetValue(p.ClientId, out var n) ? n : "",
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            DayCount = p.DayCount,
            Price = p.Price,
            Currency = p.Currency,
            PaymentStatus = p.PaymentStatus
        }).ToList();
    }
}
