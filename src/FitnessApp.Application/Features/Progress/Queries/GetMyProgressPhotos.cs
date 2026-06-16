using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Progress;
using MediatR;

namespace FitnessApp.Application.Features.Progress.Queries;

public record GetMyProgressPhotosQuery(int? PlanId = null, int Page = 1) : IRequest<PagedResult<ProgressPhotoDto>>;

public class GetMyProgressPhotosQueryHandler : IRequestHandler<GetMyProgressPhotosQuery, PagedResult<ProgressPhotoDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyProgressPhotosQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ProgressPhotoDto>> Handle(GetMyProgressPhotosQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        return await _db.ProgressPhotos
            .Where(p => p.ClientId == userId)
            .Where(p => request.PlanId == null || p.PlanId == request.PlanId)
            .OrderByDescending(p => p.TakenOn)
            .ThenByDescending(p => p.CreatedAt)
            .Select(ProgressMapping.ToDto)
            .ToPagedResultAsync(request.Page, PaginationExtensions.DefaultPageSize, cancellationToken);
    }
}
