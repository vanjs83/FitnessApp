using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Admin;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Admin.Queries;

public record GetPlansQuery : IRequest<IReadOnlyList<PlanAdminDto>>;

public class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, IReadOnlyList<PlanAdminDto>>
{
    private readonly IAppDbContext _db;
    private readonly IUserDirectory _users;

    public GetPlansQueryHandler(IAppDbContext db, IUserDirectory users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IReadOnlyList<PlanAdminDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _db.TrainingPlans
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
            .ToListAsync(cancellationToken);

        var userIds = plans.Select(p => p.TrainerId).Concat(plans.Select(p => p.ClientId)).Distinct();
        var names = await _users.GetDisplayNamesAsync(userIds, cancellationToken);

        return plans.Select(p => new PlanAdminDto
        {
            Id = p.Id,
            Name = p.Name,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            DayCount = p.DayCount,
            PerformedSetCount = p.PerformedSetCount,
            ClientName = names.TryGetValue(p.ClientId, out var cn) ? cn : "(nepoznat)",
            TrainerName = names.TryGetValue(p.TrainerId, out var tn) ? tn : null
        }).ToList();
    }
}
