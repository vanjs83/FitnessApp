using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Nutrition;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Queries;

public record GetNutritionTemplatesQuery : IRequest<IReadOnlyList<NutritionTemplateListItemDto>>;

public class GetNutritionTemplatesQueryHandler
    : IRequestHandler<GetNutritionTemplatesQuery, IReadOnlyList<NutritionTemplateListItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetNutritionTemplatesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<NutritionTemplateListItemDto>> Handle(
        GetNutritionTemplatesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        return await _db.NutritionPlans
            .Where(p => p.TrainerId == userId && p.IsTemplate)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new NutritionTemplateListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Notes = p.Notes,
                DayCount = p.Days.Count,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
