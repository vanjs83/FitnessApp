using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Queries;

public record GetTrainingPlanByIdQuery(int Id) : IRequest<Result<TrainingPlanDetailDto>>;

public class GetTrainingPlanByIdQueryHandler
    : IRequestHandler<GetTrainingPlanByIdQuery, Result<TrainingPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetTrainingPlanByIdQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<TrainingPlanDetailDto>> Handle(
        GetTrainingPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        var plan = await _db.TrainingPlans
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.Exercise)
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.Completions)
            .Include(p => p.Days).ThenInclude(d => d.Exercises).ThenInclude(pe => pe.PerformedSets)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<TrainingPlanDetailDto>.NotFound();

        if (plan.IsTemplate)
        {
            if (!isTrainer || plan.TrainerId != userId) return Result<TrainingPlanDetailDto>.Forbidden();
            return Result<TrainingPlanDetailDto>.Success(TrainingMapping.MapDetail(plan, "", false));
        }

        if (isTrainer && plan.TrainerId != userId) return Result<TrainingPlanDetailDto>.Forbidden();
        if (!isTrainer && plan.ClientId != userId) return Result<TrainingPlanDetailDto>.Forbidden();

        var client = plan.ClientId != null ? await _users.FindAsync(plan.ClientId, cancellationToken) : null;
        if (!isTrainer && client?.TrainerId != plan.TrainerId) return Result<TrainingPlanDetailDto>.Forbidden();

        var isLocked = !isTrainer && plan.PaymentStatus != PaymentStatus.Approved;
        return Result<TrainingPlanDetailDto>.Success(TrainingMapping.MapDetail(plan, client?.DisplayName ?? "", isLocked));
    }
}
