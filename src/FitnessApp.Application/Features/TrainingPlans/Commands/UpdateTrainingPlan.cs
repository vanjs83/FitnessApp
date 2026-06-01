using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

public record UpdateTrainingPlanCommand(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string? TrainerExpectations,
    decimal Price,
    string Currency) : IRequest<Result>;

public class UpdateTrainingPlanCommandHandler : IRequestHandler<UpdateTrainingPlanCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateTrainingPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateTrainingPlanCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
            return Result.Fail(ResultError.Validation, "End date must be after start date.");

        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result.NotFound();
        if (plan.TrainerId != _currentUser.UserId) return Result.Forbidden();

        plan.Name = request.Name;
        plan.StartDate = request.StartDate;
        plan.EndDate = request.EndDate;
        plan.TrainerExpectations = request.TrainerExpectations;
        plan.Price = request.Price;
        plan.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency;

        if (plan.Price <= 0 && plan.PaymentStatus != PaymentStatus.Approved)
        {
            plan.PaymentStatus = PaymentStatus.Approved;
            plan.ApprovedAt ??= DateTime.UtcNow;
        }
        else if (plan.Price > 0 && plan.PaymentStatus == PaymentStatus.Approved && plan.ApprovedAt == null)
        {
            plan.PaymentStatus = PaymentStatus.Pending;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
