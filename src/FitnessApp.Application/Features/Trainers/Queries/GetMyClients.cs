using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Trainers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Trainers.Queries;

public record GetMyClientsQuery(int Page = 1, string? Search = null) : IRequest<PagedResult<ClientListItemDto>>;

public class GetMyClientsQueryHandler : IRequestHandler<GetMyClientsQuery, PagedResult<ClientListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetMyClientsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<PagedResult<ClientListItemDto>> Handle(GetMyClientsQuery request, CancellationToken cancellationToken)
    {
        var clients = await _users.GetClientsOfAsync(_currentUser.UserId, cancellationToken);
        var ids = clients.Select(c => c.Id).ToList();

        var planCounts = await _db.TrainingPlans
            .Where(p => ids.Contains(p.ClientId!))
            .GroupBy(p => p.ClientId!)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);

        var setCounts = await _db.PerformedSets
            .Where(ps => ids.Contains(ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId!))
            .GroupBy(ps => ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId!)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);

        var search = request.Search?.Trim();

        return clients
            .Where(c => string.IsNullOrEmpty(search)
                || (c.FullName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (c.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
            .Select(c => new ClientListItemDto
            {
                Id = c.Id,
                Email = c.Email ?? "",
                FullName = c.FullName,
                CreatedAt = c.CreatedAt ?? default,
                PlanCount = planCounts.TryGetValue(c.Id, out var pc) ? pc : 0,
                PerformedSetCount = setCounts.TryGetValue(c.Id, out var sc) ? sc : 0,
                ProfileImageUrl = c.ProfileImageUrl
            })
            .ToList()
            .ToPagedResult(request.Page, PaginationExtensions.DefaultPageSize);
    }
}
