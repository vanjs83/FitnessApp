using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Admin;
using MediatR;

namespace FitnessApp.Application.Features.Admin.Queries;

public record GetPlansQuery(int Page = 1) : IRequest<PagedResult<PlanAdminDto>>;

public class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, PagedResult<PlanAdminDto>>
{
    private readonly IAppDbContext _db;
    private readonly IUserDirectory _users;

    public GetPlansQueryHandler(IAppDbContext db, IUserDirectory users)
    {
        _db = db;
        _users = users;
    }

    public async Task<PagedResult<PlanAdminDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var page = await _db.TrainingPlans
            .OrderByDescending(p => p.StartDate)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
                DayCount = p.Days.Count,
                PerformedSetCount = p.Days.SelectMany(d => d.Exercises).SelectMany(pe => pe.PerformedSets).Count(),
                p.TrainerId,
                p.ClientId
            })
            .ToPagedResultAsync(request.Page, PaginationExtensions.DefaultPageSize, cancellationToken);

        var userIds = page.Items.Select(p => p.TrainerId)
            .Concat(page.Items.Select(p => p.ClientId))
            .Where(id => id != null)
            .Cast<string>()
            .Distinct();
        var names = await _users.GetDisplayNamesAsync(userIds, cancellationToken);

        var items = page.Items.Select(p => new PlanAdminDto
        {
            Id = p.Id,
            Name = p.Name,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            DayCount = p.DayCount,
            PerformedSetCount = p.PerformedSetCount,
            ClientName = p.ClientId != null && names.TryGetValue(p.ClientId, out var cn) ? cn : "(nepoznat)",
            TrainerName = names.TryGetValue(p.TrainerId, out var tn) ? tn : null
        }).ToList();

        return PagedResult<PlanAdminDto>.Create(items, page.Page, page.PageSize, page.TotalCount);
    }
}
