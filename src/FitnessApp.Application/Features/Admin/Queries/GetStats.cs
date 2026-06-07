using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Admin;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Admin.Queries;

public record GetAdminStatsQuery : IRequest<AdminStatsDto>;

public class GetAdminStatsQueryHandler : IRequestHandler<GetAdminStatsQuery, AdminStatsDto>
{
    private readonly IIdentityAdminService _identity;
    private readonly IAppDbContext _db;

    public GetAdminStatsQueryHandler(IIdentityAdminService identity, IAppDbContext db)
    {
        _identity = identity;
        _db = db;
    }

    public async Task<AdminStatsDto> Handle(GetAdminStatsQuery request, CancellationToken cancellationToken)
    {
        var trainers = await _identity.GetUsersInRoleAsync(Roles.Trainer, cancellationToken);
        var clients = await _identity.GetUsersInRoleAsync(Roles.Client, cancellationToken);

        return new AdminStatsDto
        {
            TrainersCount = trainers.Count,
            ClientsCount = clients.Count,
            ClientsWithoutTrainer = clients.Count(c => c.TrainerId == null),
            PlansCount = await _db.TrainingPlans.CountAsync(cancellationToken),
            PerformedSetsCount = await _db.PerformedSets.CountAsync(cancellationToken),
            ExercisesCount = await _db.Exercises.CountAsync(cancellationToken)
        };
    }
}
