using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Queries;

public record GetSharedNutritionPlanPdfQuery(string Token) : IRequest<Result<FileDownload>>;

public class GetSharedNutritionPlanPdfQueryHandler : IRequestHandler<GetSharedNutritionPlanPdfQuery, Result<FileDownload>>
{
    public const string ShareKind = "nutrition";

    private readonly IAppDbContext _db;
    private readonly IUserDirectory _users;
    private readonly INutritionPlanPdfService _pdf;
    private readonly IPlanShareTokenService _shareTokens;

    public GetSharedNutritionPlanPdfQueryHandler(
        IAppDbContext db, IUserDirectory users, INutritionPlanPdfService pdf, IPlanShareTokenService shareTokens)
    {
        _db = db;
        _users = users;
        _pdf = pdf;
        _shareTokens = shareTokens;
    }

    public async Task<Result<FileDownload>> Handle(GetSharedNutritionPlanPdfQuery request, CancellationToken cancellationToken)
    {
        if (!_shareTokens.TryValidateForKind(ShareKind, request.Token, out var planId))
            return Result<FileDownload>.NotFound("Link expired or invalid.");

        var plan = await _db.NutritionPlans
            .Include(p => p.Days).ThenInclude(d => d.Meals).ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan == null || plan.IsTemplate) return Result<FileDownload>.NotFound();

        var ids = new List<string> { plan.TrainerId };
        if (plan.ClientId != null) ids.Add(plan.ClientId);
        var names = await _users.GetDisplayNamesAsync(ids, cancellationToken);

        string? clientName = plan.ClientId != null && names.TryGetValue(plan.ClientId, out var c) ? c : null;
        names.TryGetValue(plan.TrainerId, out var trainerName);

        var bytes = _pdf.Generate(plan, clientName, trainerName);
        return Result<FileDownload>.Success(new FileDownload(bytes, "application/pdf", NutritionMapping.PdfFileName(plan)));
    }
}
