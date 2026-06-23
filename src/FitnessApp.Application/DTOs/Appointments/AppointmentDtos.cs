using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.DTOs.Appointments;

public class AppointmentDto
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public string? ClientId { get; set; }

    /// <summary>The other party from the caller's perspective (their client, or their trainer).</summary>
    public string CounterpartId { get; set; } = string.Empty;
    public string CounterpartName { get; set; } = string.Empty;

    // Group sessions: GroupId set, ClientId null. CounterpartName carries the group name.
    public bool IsGroup { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int MemberCount { get; set; }

    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime EndsAt { get; set; }

    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Trainer books a session: individual (set <see cref="ClientId"/>) or for a whole group
/// (set <see cref="GroupId"/> instead). Exactly one of the two is provided.
/// </summary>
public class CreateAppointmentRequest
{
    public string? ClientId { get; set; }
    public int? GroupId { get; set; }
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public AppointmentType Type { get; set; } = AppointmentType.InPerson;
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Client asks their trainer for a slot (trainer must confirm).</summary>
public class RequestAppointmentRequest
{
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public AppointmentType Type { get; set; } = AppointmentType.InPerson;
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public class UpdateAppointmentRequest
{
    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public AppointmentType Type { get; set; } = AppointmentType.InPerson;
    public string? Location { get; set; }
    public string? Notes { get; set; }
}
