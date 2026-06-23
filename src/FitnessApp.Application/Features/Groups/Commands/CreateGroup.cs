using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Groups.Commands;

/// <summary>Trainer creates a named group from a subset of their own clients.</summary>
public record CreateGroupCommand(string Name, IReadOnlyList<string> ClientIds) : IRequest<Result<TrainingGroupDto>>;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Result<TrainingGroupDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public CreateGroupCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<TrainingGroupDto>> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var trainerId = _currentUser.UserId;

        // Distinct, non-empty ids only — and every one must be one of this trainer's clients.
        var requestedIds = request.ClientIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var ownClientIds = (await _users.GetClientsOfAsync(trainerId, cancellationToken)).Select(c => c.Id).ToHashSet();
        if (requestedIds.Any(id => !ownClientIds.Contains(id)))
            return Result<TrainingGroupDto>.Fail(ResultError.Validation, "One or more clients are not yours.");

        var group = new TrainingGroup
        {
            TrainerId = trainerId,
            Name = request.Name.Trim(),
            Members = requestedIds.Select(id => new TrainingGroupMember { ClientId = id }).ToList()
        };

        _db.TrainingGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<TrainingGroupDto>.Success(await GroupMapping.ToDtoAsync(group, _users, cancellationToken));
    }
}
