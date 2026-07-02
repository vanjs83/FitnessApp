using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Messaging;
using FitnessApp.Application.Features.ScheduledMessages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.ScheduledMessages.Queries;

/// <summary>The caller's own scheduled messages (newest send time first).</summary>
public record GetMyScheduledMessagesQuery : IRequest<IReadOnlyList<ScheduledMessageDto>>;

public class GetMyScheduledMessagesQueryHandler : IRequestHandler<GetMyScheduledMessagesQuery, IReadOnlyList<ScheduledMessageDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyScheduledMessagesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ScheduledMessageDto>> Handle(GetMyScheduledMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var messages = await _db.ScheduledMessages
            .Where(m => m.SenderId == userId)
            .OrderByDescending(m => m.SendAtUtc)
            .ToListAsync(cancellationToken);

        return messages.Select(m => m.ToDto()).ToList();
    }
}
