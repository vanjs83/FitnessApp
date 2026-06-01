using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Queries;

public record GetSharedTrainingPlanPdfQuery(string Token) : IRequest<Result<FileDownload>>;

public class GetSharedTrainingPlanPdfQueryHandler : IRequestHandler<GetSharedTrainingPlanPdfQuery, Result<FileDownload>>
{
    public const string ShareKind = "training";

    private readonly IAppDbContext _db;
    private readonly IUserDirectory _users;
    private readonly ITrainingPlanPdfService _pdf;
    private readonly IPlanShareTokenService _shareTokens;

    public GetSharedTrainingPlanPdfQueryHandler(
        IAppDbContext db, IUserDirectory users, ITrainingPlanPdfService pdf, IPlanShareTokenService shareTokens)
    {
        _db = db;
        _users = users;
        _pdf = pdf;
        _shareTokens = shareTokens;
    }

    public async Task<Result<FileDownload>> Handle(GetSharedTrainingPlanPdfQuery request, CancellationToken cancellationToken)
    {
        if (!_shareTokens.TryValidateForKind(ShareKind, request.Token, out var planId))
            return Result<FileDownload>.NotFound("Link expired or invalid.");

        var plan = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.Exercise)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan == null) return Result<FileDownload>.NotFound();
        if (plan.PaymentStatus != PaymentStatus.Approved)
            return Result<FileDownload>.NotFound("Plan is not available.");

        var ids = new List<string> { plan.TrainerId };
        if (plan.ClientId != null) ids.Add(plan.ClientId);
        var names = await _users.GetDisplayNamesAsync(ids, cancellationToken);

        string? clientName = plan.ClientId != null && names.TryGetValue(plan.ClientId, out var c) ? c : null;
        names.TryGetValue(plan.TrainerId, out var trainerName);

        var bytes = _pdf.Generate(plan, clientName, trainerName);
        return Result<FileDownload>.Success(new FileDownload(bytes, "application/pdf", TrainingMapping.PdfFileName(plan)));
    }
}
