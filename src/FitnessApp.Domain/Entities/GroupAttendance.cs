namespace FitnessApp.Domain.Entities;

/// <summary>
/// One group member's confirmation that they will attend a specific group session.
/// A row exists only while the client is attending; withdrawing removes it, so the
/// number of rows for an appointment is the confirmed-attendee count.
/// </summary>
public class GroupAttendance
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string ClientId { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
}
