using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;

namespace FitnessApp.Application.Features.Groups.Commands;

/// <summary>Soft-deletes a group (kept so existing group sessions still resolve their group name).</summary>
public record DeleteGroupCommand(int GroupId) : IRequest<Result>;

public class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteGroupCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        var (group, error) = await GroupGuard.LoadOwnedAsync(_db, request.GroupId, _currentUser.UserId, cancellationToken);
        if (error is not null) return Result.Fail(error.Value);

        group!.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
