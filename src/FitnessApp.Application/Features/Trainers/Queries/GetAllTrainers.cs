using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Domain.Common;
using MediatR;

namespace FitnessApp.Application.Features.Trainers.Queries;

public record GetAllTrainersQuery : IRequest<IReadOnlyList<TrainerListItemDto>>;

public class GetAllTrainersQueryHandler : IRequestHandler<GetAllTrainersQuery, IReadOnlyList<TrainerListItemDto>>
{
    private readonly IIdentityAdminService _identity;

    public GetAllTrainersQueryHandler(IIdentityAdminService identity) => _identity = identity;

    public async Task<IReadOnlyList<TrainerListItemDto>> Handle(GetAllTrainersQuery request, CancellationToken cancellationToken)
    {
        var trainers = await _identity.GetUsersInRoleAsync(Roles.Trainer, cancellationToken);
        return trainers
            .OrderBy(t => t.FullName ?? t.Email)
            .Select(t => new TrainerListItemDto
            {
                Id = t.Id,
                Email = t.Email,
                FullName = t.FullName,
                ProfileImageUrl = t.ProfileImageUrl
            })
            .ToList();
    }
}
