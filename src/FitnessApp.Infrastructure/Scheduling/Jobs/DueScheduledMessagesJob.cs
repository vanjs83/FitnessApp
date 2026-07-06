using FitnessApp.Application.Features.ScheduledMessages.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Recurring job that dispatches due scheduled messages. Just another <see cref="IScheduledJob"/> the
/// scheduler picks up — runs every minute so a message goes out close to its chosen time. Gated on the
/// same background-jobs switch as the reminders.
/// </summary>
public sealed class DueScheduledMessagesJob : IScheduledJob
{
    private readonly ISender _sender;
    private readonly ILogger<DueScheduledMessagesJob> _logger;

    public DueScheduledMessagesJob(ISender sender, ILogger<DueScheduledMessagesJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    // Default cadence; overridable per job from the "JobScheduling" config section.
    public string Cron => "* * * * *";

    public async Task Invoke()
    {
        var sent = await _sender.Send(new SendDueScheduledMessagesCommand());
        if (sent > 0) _logger.LogInformation("Dispatched {Count} scheduled message(s).", sent);
    }
}
