using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.DTOs.Appointments;

public class AppointmentDto
{
    public int Id { get; set; }
    public string TrainerId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The other party from the caller's perspective (their client, or their trainer).</summary>
    public string CounterpartId { get; set; } = string.Empty;
    public string CounterpartName { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime EndsAt { get; set; }

    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Trainer books a session for one of their clients.</summary>
public class CreateAppointmentRequest
{
    public string ClientId { get; set; } = string.Empty;
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
