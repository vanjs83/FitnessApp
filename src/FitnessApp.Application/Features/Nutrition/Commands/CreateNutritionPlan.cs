using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Nutrition.Commands;

public record CreateNutritionPlanCommand(
    string ClientId,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string? Notes,
    decimal Price,
    string Currency) : IRequest<Result<NutritionPlanDetailDto>>;

public class CreateNutritionPlanCommandHandler : IRequestHandler<CreateNutritionPlanCommand, Result<NutritionPlanDetailDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public CreateNutritionPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<NutritionPlanDetailDto>> Handle(CreateNutritionPlanCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
            return Result<NutritionPlanDetailDto>.Fail(ResultError.Validation, "End date must be after start date.");

        var client = await _users.FindAsync(request.ClientId, cancellationToken);
        if (client == null) return Result<NutritionPlanDetailDto>.Fail(ResultError.Validation, "Client not found.");
        if (client.TrainerId != _currentUser.UserId) return Result<NutritionPlanDetailDto>.Forbidden();

        var plan = new NutritionPlan
        {
            TrainerId = _currentUser.UserId,
            ClientId = request.ClientId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes,
            Price = request.Price,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            PaymentStatus = request.Price > 0 ? PaymentStatus.Pending : PaymentStatus.Approved
        };
        if (plan.PaymentStatus == PaymentStatus.Approved)
            plan.ApprovedAt = DateTime.UtcNow;

        _db.NutritionPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<NutritionPlanDetailDto>.Success(NutritionMapping.MapDetail(plan, client.DisplayName, false));
    }
}
