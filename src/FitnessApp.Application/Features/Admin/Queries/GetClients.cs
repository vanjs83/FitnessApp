using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Admin;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Admin.Queries;

public record GetClientsQuery(int Page = 1, string? Search = null) : IRequest<PagedResult<ClientAdminDto>>;

public class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, PagedResult<ClientAdminDto>>
{
    private readonly IIdentityAdminService _identity;
    private readonly IUserDirectory _users;
    private readonly IAppDbContext _db;

    public GetClientsQueryHandler(IIdentityAdminService identity, IUserDirectory users, IAppDbContext db)
    {
        _identity = identity;
        _users = users;
        _db = db;
    }

    public async Task<PagedResult<ClientAdminDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        var clients = await _identity.GetUsersInRoleAsync(Roles.Client, cancellationToken);
        var clientIds = clients.Select(c => c.Id).ToList();

        var performedCounts = await _db.PerformedSets
            .Where(ps =>
                ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId != null &&
                clientIds.Contains(ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId))
            .GroupBy(ps => ps.PlannedExercise.TrainingDay.TrainingPlan.ClientId)
            .Select(g => new { ClientId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId!, x => x.Count, cancellationToken);

        var trainerIds = clients.Where(c => c.TrainerId != null).Select(c => c.TrainerId!).Distinct();
        var trainerNames = await _users.GetDisplayNamesAsync(trainerIds, cancellationToken);

        var search = request.Search?.Trim();

        return clients
            .OrderBy(c => c.FullName ?? c.Email)
            .Select(c => new ClientAdminDto
            {
                Id = c.Id,
                Email = c.Email,
                FullName = c.FullName,
                CreatedAt = c.CreatedAt,
                TrainerName = c.TrainerId != null && trainerNames.TryGetValue(c.TrainerId, out var tn) ? tn : null,
                PerformedSetCount = performedCounts.TryGetValue(c.Id, out var pc) ? pc : 0
            })
            .Where(c => string.IsNullOrEmpty(search)
                || (c.FullName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (c.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (c.TrainerName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .ToPagedResult(request.Page, PaginationExtensions.DefaultPageSize);
    }
}
