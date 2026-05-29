using System.Security.Claims;
using FitnessApp.Api.Hubs;
using FitnessApp.Application.DTOs.Chat;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private const int MaxBodyLength = 2000;
    private const int HistoryLimit = 200;

    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public ChatController(AppDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("conversations")]
    public async Task<ActionResult<IEnumerable<ConversationDto>>> GetConversations(CancellationToken ct)
    {
        var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (me == null) return Unauthorized();

        // Partners are everyone in a trainer<->client link with me, in either direction.
        var partners = await _db.Users
            .Where(u => u.TrainerId == me.Id || (me.TrainerId != null && u.Id == me.TrainerId))
            .Select(u => new { u.Id, u.FullName, u.Email, u.ProfileImagePath })
            .ToListAsync(ct);

        var result = new List<ConversationDto>(partners.Count);
        foreach (var p in partners)
        {
            var last = await _db.ChatMessages
                .Where(m => (m.SenderId == me.Id && m.RecipientId == p.Id)
                         || (m.SenderId == p.Id && m.RecipientId == me.Id))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new { m.Body, m.SentAt })
                .FirstOrDefaultAsync(ct);

            var unread = await _db.ChatMessages
                .CountAsync(m => m.SenderId == p.Id && m.RecipientId == me.Id && m.ReadAt == null, ct);

            result.Add(new ConversationDto
            {
                PartnerId = p.Id,
                PartnerName = p.FullName ?? p.Email,
                PartnerImageUrl = p.ProfileImagePath,
                LastMessage = last?.Body,
                LastMessageAt = last?.SentAt,
                UnreadCount = unread
            });
        }

        return Ok(result
            .OrderByDescending(c => c.LastMessageAt ?? DateTime.MinValue)
            .ThenBy(c => c.PartnerName));
    }

    [HttpGet("with/{partnerId}")]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetMessages(string partnerId, [FromQuery] int? afterId, CancellationToken ct)
    {
        if (!await AreLinkedAsync(partnerId, ct)) return Forbid();

        var query = _db.ChatMessages
            .Where(m => (m.SenderId == UserId && m.RecipientId == partnerId)
                     || (m.SenderId == partnerId && m.RecipientId == UserId));

        if (afterId is > 0)
            query = query.Where(m => m.Id > afterId.Value);

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
            .ToListAsync(ct);

        messages.Reverse();
        return Ok(messages);
    }

    [HttpPost("with/{partnerId}")]
    public async Task<ActionResult<ChatMessageDto>> Send(string partnerId, SendMessageRequest request, CancellationToken ct)
    {
        var body = request.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body))
            return BadRequest(new { message = "Message cannot be empty." });
        if (body.Length > MaxBodyLength)
            return BadRequest(new { message = $"Message is too long (max {MaxBodyLength} characters)." });

        if (!await AreLinkedAsync(partnerId, ct)) return Forbid();

        var message = new ChatMessage
        {
            SenderId = UserId,
            RecipientId = partnerId,
            Body = body,
            SentAt = DateTime.UtcNow
        };
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        var dto = new ChatMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            Body = message.Body,
            SentAt = message.SentAt,
            ReadAt = message.ReadAt
        };

        // Real-time delivery to both ends (recipient sees it instantly; sender's other tabs stay in sync).
        await _hub.Clients.Users(partnerId, UserId).SendAsync("ReceiveMessage", dto, ct);

        return Ok(dto);
    }

    [HttpPost("with/{partnerId}/read")]
    public async Task<IActionResult> MarkRead(string partnerId, CancellationToken ct)
    {
        if (!await AreLinkedAsync(partnerId, ct)) return Forbid();

        var now = DateTime.UtcNow;
        await _db.ChatMessages
            .Where(m => m.SenderId == partnerId && m.RecipientId == UserId && m.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAt, now), ct);

        return NoContent();
    }

    // A conversation is allowed only between a trainer and one of their clients (either direction).
    private async Task<bool> AreLinkedAsync(string partnerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(partnerId) || partnerId == UserId) return false;
        var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct);
        var partner = await _db.Users.FirstOrDefaultAsync(u => u.Id == partnerId, ct);
        if (me == null || partner == null) return false;
        return partner.TrainerId == me.Id || me.TrainerId == partner.Id;
    }
}
