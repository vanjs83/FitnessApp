namespace FitnessApp.Domain.Entities;

public enum AppointmentStatus
{
    Requested = 0,   // client asked for a slot, awaiting trainer confirmation
    Scheduled = 1,   // confirmed by the trainer
    Completed = 2,
    Cancelled = 3,
    NoShow = 4
}

public enum AppointmentType
{
    InPerson = 0,
    Online = 1
}

/// <summary>
/// A training session booked by a trainer. It is either individual (<see cref="ClientId"/> set) or
/// for a whole group (<see cref="GroupId"/> set) — never both.
/// </summary>
public class Appointment
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;

    /// <summary>The client for an individual session; null for a group session.</summary>
    public string? ClientId { get; set; }

    /// <summary>The group for a group session; null for an individual session.</summary>
    public int? GroupId { get; set; }
    public TrainingGroup? Group { get; set; }

    public bool IsGroup => GroupId.HasValue;

    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 60;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentType Type { get; set; } = AppointmentType.InPerson;

    /// <summary>Physical address (in-person) or a meeting link (online).</summary>
    public string? Location { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set once the pre-session reminder push has been sent, so it fires only once.</summary>
    public DateTime? ReminderSentAt { get; set; }

    public DateTime EndsAt => StartsAt.AddMinutes(DurationMinutes);
}
