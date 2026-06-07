using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainerRequests.Commands;

public record CancelTrainerRequestCommand(int Id) : IRequest<Result>;

public class CancelTrainerRequestCommandHandler : IRequestHandler<CancelTrainerRequestCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CancelTrainerRequestCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelTrainerRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TrainerRequests.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity == null) return Result.NotFound();
        if (entity.ClientId != _currentUser.UserId) return Result.Forbidden();
        if (entity.Status != TrainerRequestStatus.Pending)
            return Result.Fail(ResultError.Validation, "Only a pending request can be cancelled.");

        entity.Status = TrainerRequestStatus.Cancelled;
        entity.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
