using FitnessApp.Application.Features.Appointments.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitnessApp.Infrastructure.Notifications;

/// <summary>
/// Periodically sends pre-session push reminders. Runs on a timer; each tick opens its own DI scope
/// (the reminder handler and its dependencies are scoped) and is wrapped so a failure never stops
/// the loop.
/// </summary>
public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RemindersSettings _settings;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(
        IServiceScopeFactory scopeFactory,
        IOptions<RemindersSettings> settings,
        ILogger<AppointmentReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Appointment reminder worker is disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.PollMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var sent = await sender.Send(
                    new SendDueAppointmentRemindersCommand(_settings.LeadMinutes, _settings.TimeZoneId), stoppingToken);
                if (sent > 0) _logger.LogInformation("Sent {Count} appointment reminder(s).", sent);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Appointment reminder tick failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
