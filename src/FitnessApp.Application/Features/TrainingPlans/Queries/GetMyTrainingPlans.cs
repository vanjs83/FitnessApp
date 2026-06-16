using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using MediatR;

namespace FitnessApp.Application.Features.TrainingPlans.Queries;

public record GetMyTrainingPlansQuery(int Page = 1) : IRequest<PagedResult<TrainingPlanListItemDto>>;

public class GetMyTrainingPlansQueryHandler
    : IRequestHandler<GetMyTrainingPlansQuery, PagedResult<TrainingPlanListItemDto>>
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

    public async Task<PagedResult<TrainingPlanListItemDto>> Handle(
        GetMyTrainingPlansQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var page = await _db.TrainingPlans
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
            .ToPagedResultAsync(request.Page, PaginationExtensions.DefaultPageSize, cancellationToken);

        var names = await _users.GetDisplayNamesAsync(
            page.Items.Where(p => p.ClientId != null).Select(p => p.ClientId!), cancellationToken);

        var items = page.Items.Select(p => new TrainingPlanListItemDto
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

        return PagedResult<TrainingPlanListItemDto>.Create(items, page.Page, page.PageSize, page.TotalCount);
    }
}
