using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Chat.Queries;

public record GetConversationsQuery : IRequest<IReadOnlyList<ConversationDto>>;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserDirectory _users;

    public GetConversationsQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IUserDirectory users)
    {
        _db = db;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<IReadOnlyList<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var meId = _currentUser.UserId;
        var partners = await _users.GetLinkedPartnersAsync(meId, cancellationToken);

        var result = new List<ConversationDto>(partners.Count);
        foreach (var p in partners)
        {
            var last = await _db.ChatMessages
                .Where(m => (m.SenderId == meId && m.RecipientId == p.Id)
                         || (m.SenderId == p.Id && m.RecipientId == meId))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new { m.Body, m.SentAt })
                .FirstOrDefaultAsync(cancellationToken);

            var unread = await _db.ChatMessages
                .CountAsync(m => m.SenderId == p.Id && m.RecipientId == meId && m.ReadAt == null, cancellationToken);

            result.Add(new ConversationDto
            {
                PartnerId = p.Id,
                PartnerName = p.DisplayName,
                PartnerImageUrl = p.ProfileImageUrl,
                LastMessage = last?.Body,
                LastMessageAt = last?.SentAt,
                UnreadCount = unread
            });
        }

        return result
            .OrderByDescending(c => c.LastMessageAt ?? DateTime.MinValue)
            .ThenBy(c => c.PartnerName)
            .ToList();
    }
}
