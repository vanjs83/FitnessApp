using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

public record CreateTrainingPlanCommand(
    string ClientId,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string? TrainerExpectations,
    decimal Price,
    string Currency) : IRequest<Result<TrainingPlanDetailDto>>;

public class CreateTrainingPlanCommandHandler : IRequestHandler<CreateTrainingPlanCommand, Result<TrainingPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public CreateTrainingPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<TrainingPlanDetailDto>> Handle(CreateTrainingPlanCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
            return Result<TrainingPlanDetailDto>.Fail(ResultError.Validation, "End date must be after start date.");

        var client = await _users.FindAsync(request.ClientId, cancellationToken);
        if (client == null) return Result<TrainingPlanDetailDto>.Fail(ResultError.Validation, "Client not found.");
        if (client.TrainerId != _currentUser.UserId) return Result<TrainingPlanDetailDto>.Forbidden();

        var plan = new TrainingPlan
        {
            TrainerId = _currentUser.UserId,
            ClientId = request.ClientId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TrainerExpectations = request.TrainerExpectations,
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            PaymentStatus = request.Price > 0 ? PaymentStatus.Pending : PaymentStatus.Approved
        };
        if (plan.PaymentStatus == PaymentStatus.Approved)
            plan.ApprovedAt = DateTime.UtcNow;

        _db.TrainingPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<TrainingPlanDetailDto>.Success(TrainingMapping.MapDetail(plan, client.DisplayName, false));
    }
}
