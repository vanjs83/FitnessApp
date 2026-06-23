namespace FitnessApp.Infrastructure.Notifications;

public class RemindersSettings
{
    /// <summary>Master switch for the reminder background worker.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How long before a session starts the reminder is sent.</summary>
    public int LeadMinutes { get; set; } = 60;

    /// <summary>How often the worker scans for due reminders.</summary>
    public int PollMinutes { get; set; } = 5;
}
