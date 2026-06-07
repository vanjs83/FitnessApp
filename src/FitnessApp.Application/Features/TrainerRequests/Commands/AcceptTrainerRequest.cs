using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.TrainerRequests.Commands;

public record AcceptTrainerRequestCommand(int Id) : IRequest<Result<string>>;

public class AcceptTrainerRequestCommandHandler : IRequestHandler<AcceptTrainerRequestCommand, Result<string>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRelationshipService _relationship;

    public AcceptTrainerRequestCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserRelationshipService relationship)
    {
        _db = db;
        _currentUser = currentUser;
        _relationship = relationship;
    }

    public async Task<Result<string>> Handle(AcceptTrainerRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TrainerRequests.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity == null) return Result<string>.NotFound();
        if (entity.TrainerId != _currentUser.UserId) return Result<string>.Forbidden();
        if (entity.Status != TrainerRequestStatus.Pending)
            return Result<string>.Fail(ResultError.Validation, "Request is no longer pending.");

        var outcome = await _relationship.AssignTrainerAsync(entity.ClientId, _currentUser.UserId, cancellationToken);
        switch (outcome)
        {
            case AssignTrainerOutcome.ClientNotFound:
                return Result<string>.Fail(ResultError.Validation, "Client not found.");
            case AssignTrainerOutcome.ClientAlreadyHasTrainer:
                return Result<string>.Fail(ResultError.Validation, "Client already has a trainer.");
        }

        // Request fulfilled: the connection now lives on ApplicationUser.TrainerId,
        // so the request row is no longer needed and is removed.
        _db.TrainerRequests.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.ClientId);
    }
}
