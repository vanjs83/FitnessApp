using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainerRequests.Commands;

public record RejectTrainerRequestCommand(int Id) : IRequest<Result>;

public class RejectTrainerRequestCommandHandler : IRequestHandler<RejectTrainerRequestCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RejectTrainerRequestCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RejectTrainerRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TrainerRequests.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity == null) return Result.NotFound();
        if (entity.TrainerId != _currentUser.UserId) return Result.Forbidden();
        if (entity.Status != TrainerRequestStatus.Pending)
            return Result.Fail(ResultError.Validation, "Request is no longer pending.");

        // Rejected requests are removed rather than kept as history.
        _db.TrainerRequests.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
