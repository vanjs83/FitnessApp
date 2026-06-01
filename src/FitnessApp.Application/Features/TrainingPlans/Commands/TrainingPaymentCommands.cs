using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

// ===== Client claims they paid =====
public record ClaimTrainingPaymentCommand(int Id) : IRequest<Result<PaymentStatusResponse>>;

public class ClaimTrainingPaymentCommandHandler : IRequestHandler<ClaimTrainingPaymentCommand, Result<PaymentStatusResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClaimTrainingPaymentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PaymentStatusResponse>> Handle(ClaimTrainingPaymentCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
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
public record ApproveTrainingPaymentCommand(int Id) : IRequest<Result<PaymentStatusResponse>>;

public class ApproveTrainingPaymentCommandHandler : IRequestHandler<ApproveTrainingPaymentCommand, Result<PaymentStatusResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ApproveTrainingPaymentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PaymentStatusResponse>> Handle(ApproveTrainingPaymentCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<PaymentStatusResponse>.NotFound();
        if (plan.TrainerId != _currentUser.UserId) return Result<PaymentStatusResponse>.Forbidden();

        plan.PaymentStatus = PaymentStatus.Approved;
        plan.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<PaymentStatusResponse>.Success(new PaymentStatusResponse(plan.PaymentStatus.ToString()));
    }
}

// ===== Trainer revokes approval =====
public record RevokeTrainingApprovalCommand(int Id) : IRequest<Result<PaymentStatusResponse>>;

public class RevokeTrainingApprovalCommandHandler : IRequestHandler<RevokeTrainingApprovalCommand, Result<PaymentStatusResponse>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RevokeTrainingApprovalCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PaymentStatusResponse>> Handle(RevokeTrainingApprovalCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
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
