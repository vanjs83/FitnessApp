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
/// A training session booked between a trainer and one of their clients.
/// </summary>
public class Appointment
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 60;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentType Type { get; set; } = AppointmentType.InPerson;

    /// <summary>Physical address (in-person) or a meeting link (online).</summary>
    public string? Location { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime EndsAt => StartsAt.AddMinutes(DurationMinutes);
}
