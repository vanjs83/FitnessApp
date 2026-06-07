using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Chat.Commands;

public record MarkMessagesReadCommand(string PartnerId) : IRequest<Result>;

public class MarkMessagesReadCommandHandler : IRequestHandler<MarkMessagesReadCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public MarkMessagesReadCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result> Handle(MarkMessagesReadCommand request, CancellationToken cancellationToken)
    {
        var meId = _currentUser.UserId;
        if (!await _users.AreLinkedAsync(meId, request.PartnerId, cancellationToken))
            return Result.Forbidden();

        var now = DateTime.UtcNow;
        await _db.ChatMessages
            .Where(m => m.SenderId == request.PartnerId && m.RecipientId == meId && m.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAt, now), cancellationToken);

        return Result.Success();
    }
}
