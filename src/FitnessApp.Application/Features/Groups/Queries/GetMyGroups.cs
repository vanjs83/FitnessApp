using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Groups;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Groups.Queries;

/// <summary>The active groups owned by the current trainer, with their members.</summary>
public record GetMyGroupsQuery() : IRequest<IReadOnlyList<TrainingGroupDto>>;

public class GetMyGroupsQueryHandler : IRequestHandler<GetMyGroupsQuery, IReadOnlyList<TrainingGroupDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetMyGroupsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<IReadOnlyList<TrainingGroupDto>> Handle(GetMyGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await _db.TrainingGroups
            .Include(g => g.Members)
            .Where(g => g.TrainerId == _currentUser.UserId && g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        var dtos = new List<TrainingGroupDto>(groups.Count);
        foreach (var group in groups)
            dtos.Add(await GroupMapping.ToDtoAsync(group, _users, cancellationToken));

        return dtos;
    }
}
