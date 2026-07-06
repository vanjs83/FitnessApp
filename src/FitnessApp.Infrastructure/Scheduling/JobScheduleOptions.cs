namespace FitnessApp.Infrastructure.Scheduling;

/// <summary>
/// Per-job schedule settings bound from the "JobScheduling" config section, keyed by job class name.
/// Lets any job's cron and active state change from appsettings without touching code; a missing entry
/// (or empty field) falls back to the value the job ships with.
/// </summary>
public sealed class JobScheduleOptions
{
    public Dictionary<string, JobSetting> Jobs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class JobSetting
{
    /// <summary>Cron override for the job. Empty/null keeps the job's coded default.</summary>
    public string? Cron { get; set; }

    /// <summary>Whether the job runs. Null keeps the job's coded default (enabled unless it says otherwise).</summary>
    public bool? Enabled { get; set; }
}
