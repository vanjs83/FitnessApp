using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Nutrition.Commands;

/// <summary>Validates ownership and returns a share token; URL/QR building stays in the controller.</summary>
public record CreateNutritionShareTokenCommand(int Id) : IRequest<Result<string>>;

public class CreateNutritionShareTokenCommandHandler : IRequestHandler<CreateNutritionShareTokenCommand, Result<string>>
{
    private const string ShareKind = "nutrition";

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPlanShareTokenService _shareTokens;

    public CreateNutritionShareTokenCommandHandler(
        IAppDbContext db, ICurrentUserService currentUser, IPlanShareTokenService shareTokens)
    {
        _db = db;
        _currentUser = currentUser;
        _shareTokens = shareTokens;
    }

    public async Task<Result<string>> Handle(CreateNutritionShareTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var isTrainer = _currentUser.IsInRole(Roles.Trainer);

        var plan = await _db.NutritionPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null) return Result<string>.NotFound();
        if (plan.IsTemplate) return Result<string>.Fail(ResultError.Validation, "Templates cannot be shared.");
        if (isTrainer && plan.TrainerId != userId) return Result<string>.Forbidden();
        if (!isTrainer && plan.ClientId != userId) return Result<string>.Forbidden();

        var token = _shareTokens.CreateForKind(ShareKind, plan.Id);
        return Result<string>.Success(token);
    }
}
