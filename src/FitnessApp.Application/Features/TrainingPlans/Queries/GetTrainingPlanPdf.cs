using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Queries;

public record GetTrainingPlanPdfQuery(int Id) : IRequest<Result<FileDownload>>;

public class GetTrainingPlanPdfQueryHandler : IRequestHandler<GetTrainingPlanPdfQuery, Result<FileDownload>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;
    private readonly ITrainingPlanPdfService _pdf;

    public GetTrainingPlanPdfQueryHandler(
        IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users, ITrainingPlanPdfService pdf)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
        _pdf = pdf;
    }

    public async Task<Result<FileDownload>> Handle(GetTrainingPlanPdfQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        var plan = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.Exercise)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<FileDownload>.NotFound();

        if (isTrainer && plan.TrainerId != userId) return Result<FileDownload>.Forbidden();
        if (!isTrainer && plan.ClientId != userId) return Result<FileDownload>.Forbidden();
        if (!isTrainer && plan.PaymentStatus != PaymentStatus.Approved)
            return Result<FileDownload>.Fail(ResultError.Validation, "Plan is not approved.");

        var ids = new List<string> { plan.TrainerId };
        if (plan.ClientId != null) ids.Add(plan.ClientId);
        var names = await _users.GetDisplayNamesAsync(ids, cancellationToken);

        string? clientName = plan.ClientId != null && names.TryGetValue(plan.ClientId, out var c) ? c : null;
        names.TryGetValue(plan.TrainerId, out var trainerName);

        var bytes = _pdf.Generate(plan, clientName, trainerName);
        return Result<FileDownload>.Success(new FileDownload(bytes, "application/pdf", TrainingMapping.PdfFileName(plan)));
    }
}
