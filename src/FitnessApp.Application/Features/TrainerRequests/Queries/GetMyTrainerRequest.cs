using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Trainers;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainerRequests.Queries;

public record GetMyTrainerRequestQuery : IRequest<MyTrainerRequestDto?>;

public class GetMyTrainerRequestQueryHandler : IRequestHandler<GetMyTrainerRequestQuery, MyTrainerRequestDto?>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetMyTrainerRequestQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<MyTrainerRequestDto?> Handle(GetMyTrainerRequestQuery request, CancellationToken cancellationToken)
    {
        var meId = _currentUser.UserId;
        var latest = await _db.TrainerRequests
            .Where(r => r.ClientId == meId && r.Status != TrainerRequestStatus.Cancelled)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest == null) return null;

        var trainer = await _users.FindAsync(latest.TrainerId, cancellationToken);

        return new MyTrainerRequestDto
        {
            Id = latest.Id,
            TrainerId = latest.TrainerId,
            TrainerName = trainer?.DisplayName ?? "",
            TrainerImageUrl = trainer?.ProfileImageUrl,
            Status = latest.Status.ToString(),
            CreatedAt = latest.CreatedAt,
            RespondedAt = latest.RespondedAt
        };
    }
}
