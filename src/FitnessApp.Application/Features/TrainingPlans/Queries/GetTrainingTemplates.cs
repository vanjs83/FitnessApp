using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Queries;

public record GetTrainingTemplatesQuery : IRequest<IReadOnlyList<TrainingPlanTemplateListItemDto>>;

public class GetTrainingTemplatesQueryHandler
    : IRequestHandler<GetTrainingTemplatesQuery, IReadOnlyList<TrainingPlanTemplateListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTrainingTemplatesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TrainingPlanTemplateListItemDto>> Handle(
        GetTrainingTemplatesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        return await _db.TrainingPlans
            .Where(p => p.TrainerId == userId && p.IsTemplate)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new TrainingPlanTemplateListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                TrainerExpectations = p.TrainerExpectations,
                DayCount = p.Days.Count,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
