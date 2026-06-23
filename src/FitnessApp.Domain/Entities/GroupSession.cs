namespace FitnessApp.Domain.Entities;

/// <summary>
/// A single training session booked for a whole <see cref="TrainingGroup"/>. Reuses the
/// <see cref="AppointmentType"/>/<see cref="AppointmentStatus"/> enums; the session carries one
/// status for everyone (per-member attendance is out of scope for now).
/// </summary>
public class GroupSession
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public int GroupId { get; set; }

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

    public TrainingGroup? Group { get; set; }

    public DateTime EndsAt => StartsAt.AddMinutes(DurationMinutes);
}
