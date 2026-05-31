using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Commands;

// ===== Client claims they paid =====
public record ClaimNutritionPaymentCommand(int Id) : IRequest<Result<PaymentStatusResponse>>;

public class ClaimNutritionPaymentCommandHandler : IRequestHandler<ClaimNutritionPaymentCommand, Result<PaymentStatusResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClaimNutritionPaymentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PaymentStatusResponse>> Handle(ClaimNutritionPaymentCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<PaymentStatusResponse>.NotFound();
        if (plan.ClientId != _currentUser.UserId) return Result<PaymentStatusResponse>.Forbidden();
        if (plan.PaymentStatus == PaymentStatus.Approved)
            return Result<PaymentStatusResponse>.Fail(ResultError.Validation, "Plan is already approved.");

        plan.PaymentStatus = PaymentStatus.PaymentClaimed;
        plan.PaymentClaimedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<PaymentStatusResponse>.Success(new PaymentStatusResponse(plan.PaymentStatus.ToString()));
    }
}

// ===== Trainer approves payment =====
public record ApproveNutritionPaymentCommand(int Id) : IRequest<Result<PaymentStatusResponse>>;

public class ApproveNutritionPaymentCommandHandler : IRequestHandler<ApproveNutritionPaymentCommand, Result<PaymentStatusResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ApproveNutritionPaymentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PaymentStatusResponse>> Handle(ApproveNutritionPaymentCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<PaymentStatusResponse>.NotFound();
        if (plan.TrainerId != _currentUser.UserId) return Result<PaymentStatusResponse>.Forbidden();

        plan.PaymentStatus = PaymentStatus.Approved;
        plan.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<PaymentStatusResponse>.Success(new PaymentStatusResponse(plan.PaymentStatus.ToString()));
    }
}

// ===== Trainer revokes approval =====
public record RevokeNutritionApprovalCommand(int Id) : IRequest<Result<PaymentStatusResponse>>;

public class RevokeNutritionApprovalCommandHandler : IRequestHandler<RevokeNutritionApprovalCommand, Result<PaymentStatusResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RevokeNutritionApprovalCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PaymentStatusResponse>> Handle(RevokeNutritionApprovalCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<PaymentStatusResponse>.NotFound();
        if (plan.TrainerId != _currentUser.UserId) return Result<PaymentStatusResponse>.Forbidden();
        if (plan.Price <= 0)
            return Result<PaymentStatusResponse>.Fail(ResultError.Validation, "A free plan cannot be locked.");

        plan.PaymentStatus = PaymentStatus.Pending;
        plan.ApprovedAt = null;
        plan.PaymentClaimedAt = null;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<PaymentStatusResponse>.Success(new PaymentStatusResponse(plan.PaymentStatus.ToString()));
    }
}
