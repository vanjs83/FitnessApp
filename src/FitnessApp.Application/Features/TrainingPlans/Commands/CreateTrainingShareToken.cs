using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainingPlans.Commands;

/// <summary>Validates ownership and returns a share token; URL/QR building stays in the controller.</summary>
public record CreateTrainingShareTokenCommand(int Id) : IRequest<Result<string>>;

public class CreateTrainingShareTokenCommandHandler : IRequestHandler<CreateTrainingShareTokenCommand, Result<string>>
{
    private const string ShareKind = "training";

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPlanShareTokenService _shareTokens;

    public CreateTrainingShareTokenCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, IPlanShareTokenService shareTokens)
    {
        _db = db;
        _currentUser = currentUser;
        _shareTokens = shareTokens;
    }

    public async Task<Result<string>> Handle(CreateTrainingShareTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        var plan = await _db.TrainingPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<string>.NotFound();

        if (isTrainer)
        {
            if (plan.TrainerId != userId) return Result<string>.Forbidden();
        }
        else
        {
            if (plan.ClientId != userId) return Result<string>.Forbidden();
            if (plan.PaymentStatus != PaymentStatus.Approved)
                return Result<string>.Fail(ResultError.Validation, "Plan is not approved.");
        }

        var token = _shareTokens.CreateForKind(ShareKind, plan.Id);
        return Result<string>.Success(token);
    }
}
