using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Groups;
using FitnessApp.Application.Features.Trainers;
using FitnessApp.Domain.Entities;
using MediatR;

namespace FitnessApp.Application.Features.Groups.Commands;

/// <summary>Adds one of the trainer's clients to an existing group.</summary>
public record AddGroupMemberCommand(int GroupId, string ClientId) : IRequest<Result<TrainingGroupDto>>;

public class AddGroupMemberCommandHandler : IRequestHandler<AddGroupMemberCommand, Result<TrainingGroupDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public AddGroupMemberCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<TrainingGroupDto>> Handle(AddGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var trainerId = _currentUser.UserId;

        var (group, error) = await GroupGuard.LoadOwnedAsync(_db, request.GroupId, trainerId, cancellationToken);
        if (error is not null) return Result<TrainingGroupDto>.Fail(error.Value);

        var clientError = await TrainerGuard.CheckOwnClientAsync(_users, request.ClientId, trainerId, cancellationToken);
        if (clientError is not null) return Result<TrainingGroupDto>.Fail(clientError.Value);

        if (group!.Members.All(m => m.ClientId != request.ClientId))
        {
            group.Members.Add(new TrainingGroupMember { GroupId = group.Id, ClientId = request.ClientId });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result<TrainingGroupDto>.Success(await GroupMapping.ToDtoAsync(group, _users, cancellationToken));
    }
}
