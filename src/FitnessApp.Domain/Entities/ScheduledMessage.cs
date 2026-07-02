namespace FitnessApp.Domain.Entities;

public enum ScheduledMessageChannel
{
    Push = 0,
    Email = 1
}

public enum ScheduledMessageAudience
{
    Single = 0,   // a single
    Group = 1     // every member of a training group (GroupId)
}

public enum ScheduledMessageStatus
{
    Pending = 0,    // waiting for SendAtUtc
    Sent = 1,
    Cancelled = 2,
    Failed = 3
}

/// <summary>
/// A push or email message a trainer or admin schedules to be delivered at a future time. A recurring
/// job (SendDueScheduledMessages) picks up due <see cref="ScheduledMessageStatus.Pending"/> rows and
/// dispatches them. Persisted (not just queued) so a restart never loses a scheduled send.
/// </summary>
public class ScheduledMessage
{
    public int Id { get; set; }

    /// <summary>The trainer or admin who scheduled the message.</summary>
    public string SenderId { get; set; } = string.Empty;

    public ScheduledMessageChannel Channel { get; set; }
    public ScheduledMessageAudience Audience { get; set; }

    /// <summary>Target client when <see cref="Audience"/> is Client; otherwise null.</summary>
    public string? UserId { get; set; }

    /// <summary>Target group when <see cref="Audience"/> is Group; otherwise null.</summary>
    public int? GroupId { get; set; }
    public TrainingGroup? Group { get; set; }

    /// <summary>Email subject / push title.</summary>
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public DateTime SendAtUtc { get; set; }

    public ScheduledMessageStatus Status { get; set; } = ScheduledMessageStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }

    /// <summary>Failure detail when <see cref="Status"/> is Failed.</summary>
    public string? Error { get; set; }
}
