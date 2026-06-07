using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Progress;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Progress.Queries;

public record GetMyProgressPhotosQuery : IRequest<IReadOnlyList<ProgressPhotoDto>>;

public class GetMyProgressPhotosQueryHandler : IRequestHandler<GetMyProgressPhotosQuery, IReadOnlyList<ProgressPhotoDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyProgressPhotosQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProgressPhotoDto>> Handle(GetMyProgressPhotosQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        return await _db.ProgressPhotos
            .Where(p => p.ClientId == userId)
            .OrderByDescending(p => p.TakenOn)
            .ThenByDescending(p => p.CreatedAt)
            .Select(ProgressMapping.ToDto)
            .ToListAsync(cancellationToken);
    }
}
