using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainerRequests.Queries;

public record GetIncomingTrainerRequestsQuery : IRequest<IReadOnlyList<IncomingTrainerRequestDto>>;

public class GetIncomingTrainerRequestsQueryHandler : IRequestHandler<GetIncomingTrainerRequestsQuery, IReadOnlyList<IncomingTrainerRequestDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetIncomingTrainerRequestsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<IReadOnlyList<IncomingTrainerRequestDto>> Handle(GetIncomingTrainerRequestsQuery request, CancellationToken cancellationToken)
    {
        var meId = _currentUser.UserId;
        var pending = await _db.TrainerRequests
            .Where(r => r.TrainerId == meId && r.Status == TrainerRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new { r.Id, r.ClientId, r.CreatedAt })
            .ToListAsync(cancellationToken);

        var result = new List<IncomingTrainerRequestDto>(pending.Count);
        foreach (var r in pending)
        {
            var client = await _users.FindAsync(r.ClientId, cancellationToken);
            result.Add(new IncomingTrainerRequestDto
            {
                Id = r.Id,
                ClientId = r.ClientId,
                ClientName = client?.DisplayName ?? "",
                ClientEmail = client?.Email ?? "",
                ClientImageUrl = client?.ProfileImageUrl,
                CreatedAt = r.CreatedAt
            });
        }

        return result;
    }
}
