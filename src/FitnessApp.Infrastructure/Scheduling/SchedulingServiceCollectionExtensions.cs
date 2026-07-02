using Coravel;
using FitnessApp.Application.Interfaces;
using FitnessApp.Infrastructure.Scheduling.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessApp.Infrastructure.Scheduling;

/// <summary>
/// DI wiring for the scheduling module: Coravel primitives, the injectable services
/// (<see cref="IJobScheduler"/>, <see cref="IMessageScheduler"/>) and the recurring jobs.
/// </summary>
public static class SchedulingServiceCollectionExtensions
{
    public static IServiceCollection AddScheduling(this IServiceCollection services)
    {
        // Coravel primitives: Scheduler for recurring jobs, Queue for fire-and-forget dispatch.
        services.AddScheduler();
        services.AddQueue();

        // Injectable services.
        services.AddSingleton<IJobScheduler, CoravelJobScheduler>();
        services.AddSingleton<IMessageScheduler, MessageScheduler>();

        // Recurring jobs — add new ones here (DB cleanup, …).
        services.AddScheduledJob<AppointmentReminderJob>();
        services.AddScheduledJob<DueScheduledMessagesJob>();

        return services;
    }

    /// <summary>
    /// Registers a recurring job so <see cref="IJobScheduler"/> discovers it. The concrete type is
    /// registered (Coravel resolves it fresh per run) and also exposed as <see cref="IScheduledJob"/>.
    /// </summary>
    public static IServiceCollection AddScheduledJob<TJob>(this IServiceCollection services)
        where TJob : class, IScheduledJob
    {
        services.AddTransient<TJob>();
        services.AddTransient<IScheduledJob>(sp => sp.GetRequiredService<TJob>());
        return services;
    }
}
