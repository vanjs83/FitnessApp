using Coravel.Invocable;

namespace FitnessApp.Infrastructure.Scheduling;

/// <summary>
/// A recurring background job driven by the Coravel scheduler. Anything that needs to run on a
/// schedule (appointment reminders, due scheduled-message dispatch, DB cleanup, …) implements this
/// and is picked up automatically by <see cref="SchedulerExtensions.UseAppScheduler"/> — the
/// scheduler itself stays decoupled from any specific feature.
/// </summary>
public interface IScheduledJob : IInvocable
{
    /// <summary>Standard 5-field cron expression controlling how often the job runs.</summary>
    string Cron { get; }

    /// <summary>When false the job is not scheduled at all. Lets a job gate itself on config.</summary>
    bool Enabled => true;
}
