namespace FitnessApp.Infrastructure.Notifications;

public class RemindersSettings
{
    /// <summary>How long before a session starts the reminder is sent.</summary>
    public int LeadMinutes { get; set; } = 60;

    /// <summary>
    /// Timezone for evaluating "now" against the naive wall-clock StartsAt. Windows id, e.g.
    /// "Central European Standard Time". Empty = compare against UTC.
    /// </summary>
    public string? TimeZoneId { get; set; } = "Central European Standard Time";
}
