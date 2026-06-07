using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Admin;
using FitnessApp.Domain.Common;
using MediatR;

namespace FitnessApp.Application.Features.Admin.Queries;

public record GetTrainersQuery : IRequest<IReadOnlyList<TrainerAdminDto>>;

public class GetTrainersQueryHandler : IRequestHandler<GetTrainersQuery, IReadOnlyList<TrainerAdminDto>>
{
    private readonly IIdentityAdminService _identity;

    public GetTrainersQueryHandler(IIdentityAdminService identity) => _identity = identity;

    public async Task<IReadOnlyList<TrainerAdminDto>> Handle(GetTrainersQuery request, CancellationToken cancellationToken)
    {
        var trainers = await _identity.GetUsersInRoleAsync(Roles.Trainer, cancellationToken);
        var clients = await _identity.GetUsersInRoleAsync(Roles.Client, cancellationToken);

        var clientCounts = clients
            .Where(c => c.TrainerId != null)
            .GroupBy(c => c.TrainerId!)
            .ToDictionary(g => g.Key, g => g.Count());

        return trainers
            .OrderBy(t => t.FullName ?? t.Email)
            .Select(t => new TrainerAdminDto
            {
                Id = t.Id,
                Email = t.Email,
                FullName = t.FullName,
                CreatedAt = t.CreatedAt,
                ClientCount = clientCounts.TryGetValue(t.Id, out var c) ? c : 0
            })
            .ToList();
    }
}
