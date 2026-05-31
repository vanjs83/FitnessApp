using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Queries;

public record GetNutritionPlanPdfQuery(int Id) : IRequest<Result<FileDownload>>;

public class GetNutritionPlanPdfQueryHandler : IRequestHandler<GetNutritionPlanPdfQuery, Result<FileDownload>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly INutritionPlanPdfService _pdf;

    public GetNutritionPlanPdfQueryHandler(
        IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users, INutritionPlanPdfService pdf)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
        _pdf = pdf;
    }

    public async Task<Result<FileDownload>> Handle(GetNutritionPlanPdfQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        var plan = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<FileDownload>.NotFound();

        if (plan.IsTemplate)
        {
            if (!isTrainer || plan.TrainerId != userId) return Result<FileDownload>.Forbidden();
        }
        else
        {
            if (isTrainer && plan.TrainerId != userId) return Result<FileDownload>.Forbidden();
            if (!isTrainer && plan.ClientId != userId) return Result<FileDownload>.Forbidden();
        }

        var ids = new List<string> { plan.TrainerId };
        if (plan.ClientId != null) ids.Add(plan.ClientId);
        var names = await _users.GetDisplayNamesAsync(ids, cancellationToken);

        string? clientName = plan.ClientId != null && names.TryGetValue(plan.ClientId, out var c) ? c : null;
        names.TryGetValue(plan.TrainerId, out var trainerName);

        var bytes = _pdf.Generate(plan, clientName, trainerName);
        return Result<FileDownload>.Success(new FileDownload(bytes, "application/pdf", NutritionMapping.PdfFileName(plan)));
    }
}
