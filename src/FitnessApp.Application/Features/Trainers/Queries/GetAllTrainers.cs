using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Domain.Common;
using MediatR;

namespace FitnessApp.Application.Features.Trainers.Queries;

public record GetAllTrainersQuery(int Page = 1, string? Search = null) : IRequest<PagedResult<TrainerListItemDto>>;

public class GetAllTrainersQueryHandler : IRequestHandler<GetAllTrainersQuery, PagedResult<TrainerListItemDto>>
{
    private readonly IIdentityAdminService _identity;

    public GetAllTrainersQueryHandler(IIdentityAdminService identity) => _identity = identity;

    public async Task<PagedResult<TrainerListItemDto>> Handle(GetAllTrainersQuery request, CancellationToken cancellationToken)
    {
        var trainers = await _identity.GetUsersInRoleAsync(Roles.Trainer, cancellationToken);
        var search = request.Search?.Trim();

        return trainers
            .Where(t => string.IsNullOrEmpty(search)
                || (t.FullName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (t.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.FullName ?? t.Email)
            .Select(t => new TrainerListItemDto
            {
                Id = t.Id,
                Email = t.Email,
                FullName = t.FullName,
                ProfileImageUrl = t.ProfileImageUrl
            })
            .ToList()
            .ToPagedResult(request.Page, PaginationExtensions.DefaultPageSize);
    }
}
