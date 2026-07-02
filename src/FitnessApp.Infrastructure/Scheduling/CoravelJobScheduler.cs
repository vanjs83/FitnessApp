using Coravel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Infrastructure.Scheduling;

/// <summary>
/// Injectable implementation of <see cref="IJobScheduler"/>. Discovers every registered
/// <see cref="IScheduledJob"/> and wires it into Coravel by its own cron — no job type is referenced
/// here, so the scheduler is reusable for anything (reminders, due scheduled messages, cleanup, …).
/// </summary>
public sealed class CoravelJobScheduler : IJobScheduler
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CoravelJobScheduler> _logger;

    public CoravelJobScheduler(IServiceProvider services, ILogger<CoravelJobScheduler> logger)
    {
        _services = services;
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
                .Where(j => j.Enabled)
                .Select(j => (j.GetType(), j.Cron))
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
}
