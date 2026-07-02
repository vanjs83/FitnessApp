using FitnessApp.Application.Features.ScheduledMessages.Commands;
using FitnessApp.Infrastructure.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitnessApp.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Recurring job that dispatches due scheduled messages. Just another <see cref="IScheduledJob"/> the
/// scheduler picks up — runs every minute so a message goes out close to its chosen time. Gated on the
/// same background-jobs switch as the reminders.
/// </summary>
public sealed class DueScheduledMessagesJob : IScheduledJob
{
    private readonly ISender _sender;
    private readonly RemindersSettings _settings;
    private readonly ILogger<DueScheduledMessagesJob> _logger;

    public DueScheduledMessagesJob(
        ISender sender,
        IOptions<RemindersSettings> settings,
        ILogger<DueScheduledMessagesJob> logger)
    {
        _sender = sender;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Cron => "* * * * *";

    public bool Enabled => _settings.Enabled;

    public async Task Invoke()
    {
        var sent = await _sender.Send(new SendDueScheduledMessagesCommand());
        if (sent > 0) _logger.LogInformation("Dispatched {Count} scheduled message(s).", sent);
    }
}
