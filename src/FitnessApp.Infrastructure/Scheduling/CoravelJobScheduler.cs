using Coravel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitnessApp.Infrastructure.Scheduling;

/// <summary>
/// Injectable implementation of <see cref="IJobScheduler"/>. Discovers every registered
/// <see cref="IScheduledJob"/> and wires it into Coravel by its cron — no job type is referenced
/// here, so the scheduler is reusable for anything (reminders, due scheduled messages, cleanup, …).
/// Each job's cron can be overridden from the "JobScheduling" config section by its class name.
/// </summary>
public sealed class CoravelJobScheduler : IJobScheduler
{
    private readonly IServiceProvider _services;
    private readonly JobScheduleOptions _schedules;
    private readonly ILogger<CoravelJobScheduler> _logger;

    public CoravelJobScheduler(IServiceProvider services, IOptions<JobScheduleOptions> schedules, ILogger<CoravelJobScheduler> logger)
    {
        _services = services;
        _schedules = schedules.Value;
        _logger = logger;
    }

    public void Start()
    {
        List<(Type Type, string Cron)> jobs;
        using (var scope = _services.CreateScope())
        {
            // Resolve each job once just to read its schedule/enabled flag; Coravel resolves its own
            // fresh instance per run.
            jobs = scope.ServiceProvider.GetServices<IScheduledJob>()
                .Where(Enabled)
                .Select(j => (j.GetType(), Cron(j)))
                .ToList();
        }

        if (jobs.Count == 0)
        {
            _logger.LogInformation("No enabled scheduled jobs to register.");
            return;
        }

        _services.UseScheduler(scheduler =>
            {
                foreach (var (type, cron) in jobs)
                {
                    scheduler.ScheduleInvocableType(type)
                        .Cron(cron)
                        // A slow run must never let the next tick start on top of it.
                        .PreventOverlapping(type.Name);
                    _logger.LogInformation("Scheduled job {Job} (cron: {Cron}).", type.Name, cron);
                }
            })
            // A job throwing must not tear down the scheduler loop — log and keep going.
            .OnError(ex => _logger.LogError(ex, "A scheduled job failed."));
    }

    // appsettings values win over the job's coded defaults; a missing/empty field keeps the default.
    private string Cron(IScheduledJob job)
    {
        var cron = Setting(job)?.Cron;
        return !string.IsNullOrWhiteSpace(cron) ? cron : job.Cron;
    }

    private bool Enabled(IScheduledJob job) => Setting(job)?.Enabled ?? job.Enabled;

    private JobSetting? Setting(IScheduledJob job)
        => _schedules.Jobs.TryGetValue(job.GetType().Name, out var setting) ? setting : null;
}
