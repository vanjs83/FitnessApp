using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;

namespace FitnessApp.Application.Features.Groups.Commands;

/// <summary>Removes a client from a group. No-op if they aren't a member.</summary>
public record RemoveGroupMemberCommand(int GroupId, string ClientId) : IRequest<Result>;

public class RemoveGroupMemberCommandHandler : IRequestHandler<RemoveGroupMemberCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RemoveGroupMemberCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var (group, error) = await GroupGuard.LoadOwnedAsync(_db, request.GroupId, _currentUser.UserId, cancellationToken);
        if (error is not null) return Result.Fail(error.Value);

        var member = group!.Members.FirstOrDefault(m => m.ClientId == request.ClientId);
        if (member is not null)
        {
            _db.TrainingGroupMembers.Remove(member);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
