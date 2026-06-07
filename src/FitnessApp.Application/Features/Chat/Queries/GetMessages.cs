using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Chat.Queries;

public record GetMessagesQuery(string PartnerId, int? AfterId) : IRequest<Result<IReadOnlyList<ChatMessageDto>>>;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, Result<IReadOnlyList<ChatMessageDto>>>
{
    private const int HistoryLimit = 200;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetMessagesQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var meId = _currentUser.UserId;
        if (!await _users.AreLinkedAsync(meId, request.PartnerId, cancellationToken))
            return Result<IReadOnlyList<ChatMessageDto>>.Forbidden();

        var query = _db.ChatMessages
            .Where(m => (m.SenderId == meId && m.RecipientId == request.PartnerId)
                     || (m.SenderId == request.PartnerId && m.RecipientId == meId));

        if (request.AfterId is > 0)
            query = query.Where(m => m.Id > request.AfterId.Value);

        var messages = await query
            .OrderByDescending(m => m.Id)
            .Take(HistoryLimit)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                Body = m.Body,
                SentAt = m.SentAt,
                ReadAt = m.ReadAt
            })
            .ToListAsync(cancellationToken);

        messages.Reverse();
        return Result<IReadOnlyList<ChatMessageDto>>.Success(messages);
    }
}
