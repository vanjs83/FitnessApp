using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Application.Features.ScheduledMessages.Commands;

/// <summary>
/// Dispatches every <see cref="ScheduledMessageStatus.Pending"/> message whose <c>SendAtUtc</c> has
/// passed and records the outcome. Driven by the recurring DueScheduledMessagesJob; returns how many
/// were sent. Sends are awaited here (this already runs in the background, so nothing waits on it) and
/// the row is only marked <see cref="ScheduledMessageStatus.Sent"/> after the send succeeds — so a
/// crash/restart mid-send leaves it Pending and the next scan retries it, never silently losing it.
/// </summary>
public record SendDueScheduledMessagesCommand : IRequest<int>;

public class SendDueScheduledMessagesCommandHandler : IRequestHandler<SendDueScheduledMessagesCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly IPushNotificationService _push;
    private readonly IEmailService _email;
    private readonly IUserDirectory _users;
    private readonly ILogger<SendDueScheduledMessagesCommandHandler> _logger;

    public SendDueScheduledMessagesCommandHandler(
        IAppDbContext db,
        IPushNotificationService push,
        IEmailService email,
        IUserDirectory users,
        ILogger<SendDueScheduledMessagesCommandHandler> logger)
    {
        _db = db;
        _push = push;
        _email = email;
        _users = users;
        _logger = logger;
    }

    public async Task<int> Handle(SendDueScheduledMessagesCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var due = await _db.ScheduledMessages
            .Include(m => m.Group!).ThenInclude(g => g.Members)
            .Where(m => m.Status == ScheduledMessageStatus.Pending && m.SendAtUtc <= now)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var message in due)
        {
            try
            {
                var recipientIds = message.Audience == ScheduledMessageAudience.Group
                    ? (message.Group?.Members.Select(m => m.ClientId).Distinct().ToList() ?? new List<string>())
                    : (message.UserId is null ? new List<string>() : new List<string> { message.UserId });

                foreach (var recipientId in recipientIds)
                {
                    if (message.Channel == ScheduledMessageChannel.Push)
                    {
                        await _push.SendToUserAsync(recipientId, message.Subject, message.Body, ct: cancellationToken);
                    }
                    else
                    {
                        var user = await _users.FindAsync(recipientId, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(user?.Email))
                            await _email.SendAsync(user.Email, message.Subject, message.Body);
                    }
                }

                message.Status = ScheduledMessageStatus.Sent;
                message.SentAtUtc = now;
                sent++;
            }
            catch (Exception ex)
            {
                // Leave it Pending is not possible here (we're iterating a loaded list); mark Failed so
                // it surfaces. A transient failure can be retried by re-scheduling; a crash before this
                // point leaves the row Pending for the next scan.
                message.Status = ScheduledMessageStatus.Failed;
                message.Error = ex.Message;
                _logger.LogError(ex, "Scheduled message {Id} failed to dispatch.", message.Id);
            }
        }

        if (due.Count > 0) await _db.SaveChangesAsync(cancellationToken);
        return sent;
    }
}
