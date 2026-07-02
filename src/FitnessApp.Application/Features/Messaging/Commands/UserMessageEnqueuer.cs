using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Messaging.Commands;

/// <summary>
/// Shared path for the admin "message a set of users" commands (email and push). Every send — immediate
/// or scheduled — becomes one <see cref="ScheduledMessage"/> row per recipient (Audience=Single) and is
/// dispatched by the recurring DueScheduledMessagesJob. A null/past <c>sendAtUtc</c> means "now", which
/// also kicks a due-scan immediately so the send isn't held for the next minute's cron tick.
/// </summary>
internal static class UserMessageEnqueuer
{
    public static async Task EnqueueAsync(
        IAppDbContext db,
        IMessageScheduler scheduler,
        string senderId,
        ScheduledMessageChannel channel,
        IReadOnlyList<string> userIds,
        string subject,
        string body,
        DateTime? sendAtUtc,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var when = sendAtUtc ?? now;

        foreach (var userId in userIds.Distinct())
        {
            db.ScheduledMessages.Add(new ScheduledMessage
            {
                SenderId = senderId,
                Channel = channel,
                Audience = ScheduledMessageAudience.Single,
                UserId = userId,
                Subject = subject,
                Body = body,
                SendAtUtc = when,
                Status = ScheduledMessageStatus.Pending,
                CreatedAtUtc = now
            });
        }

        await db.SaveChangesAsync(ct);

        // Immediate sends shouldn't wait for the minute-granularity cron; kick a due-scan right away.
        // Future sends are left to the recurring job.
        if (when <= now)
            scheduler.DispatchDueMessagesNow();
    }
}
