using FitnessApp.Application.Features.Appointments.Commands;
using FitnessApp.Infrastructure.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitnessApp.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Recurring job that sends due appointment reminders. Replaces the old
/// <c>AppointmentReminderService</c> <see cref="Microsoft.Extensions.Hosting.BackgroundService"/> —
/// it is just one <see cref="IScheduledJob"/> the scheduler picks up, with no coupling the other way.
/// </summary>
public sealed class AppointmentReminderJob : IScheduledJob
{
    private readonly ISender _sender;
    private readonly RemindersSettings _settings;
    private readonly ILogger<AppointmentReminderJob> _logger;

    public AppointmentReminderJob(
        ISender sender,
        IOptions<RemindersSettings> settings,
        ILogger<AppointmentReminderJob> logger)
    {
        _sender = sender;
        _settings = settings.Value;
        _logger = logger;
    }

    // PollMinutes drives the scan cadence; a */N step resets each hour, which is fine for a reminder scan.
    public string Cron => $"*/{Math.Clamp(_settings.PollMinutes, 1, 59)} * * * *";

    public bool Enabled => _settings.Enabled;

    public async Task Invoke()
    {
        var sent = await _sender.Send(
            new SendDueAppointmentRemindersCommand(_settings.LeadMinutes, _settings.TimeZoneId));
        if (sent > 0) _logger.LogInformation("Sent {Count} appointment reminder(s).", sent);
    }
}
