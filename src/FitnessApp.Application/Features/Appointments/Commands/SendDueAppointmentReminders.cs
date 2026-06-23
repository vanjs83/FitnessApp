using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Appointments.Commands;

/// <summary>
/// Sends a one-off push reminder for every scheduled session (individual or group) starting within
/// the next <paramref name="LeadMinutes"/> that hasn't been reminded yet. Driven by a background
/// worker; returns how many sessions were reminded.
/// </summary>
public record SendDueAppointmentRemindersCommand(int LeadMinutes) : IRequest<int>;

public class SendDueAppointmentRemindersCommandHandler : IRequestHandler<SendDueAppointmentRemindersCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly IPushNotificationService _push;

    public SendDueAppointmentRemindersCommandHandler(IAppDbContext db, IPushNotificationService push)
    {
        _db = db;
        _push = push;
    }

    public async Task<int> Handle(SendDueAppointmentRemindersCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var windowEnd = now.AddMinutes(request.LeadMinutes);
        var sent = 0;

        var appointments = await _db.Appointments
            .Where(a => a.Status == AppointmentStatus.Scheduled
                        && a.ReminderSentAt == null
                        && a.StartsAt > now && a.StartsAt <= windowEnd)
            .ToListAsync(cancellationToken);

        foreach (var a in appointments)
        {
            var (title, body, data) = AppointmentHelper.Reminder(a);
            await _push.SendToUserAsync(a.ClientId, title, body, data, cancellationToken);
            a.ReminderSentAt = now;
            sent++;
        }

        var sessions = await _db.GroupSessions
            .Include(s => s.Group!).ThenInclude(g => g.Members)
            .Where(s => s.Status == AppointmentStatus.Scheduled
                        && s.ReminderSentAt == null
                        && s.StartsAt > now && s.StartsAt <= windowEnd)
            .ToListAsync(cancellationToken);

        foreach (var s in sessions)
        {
            var (title, body, data) = AppointmentHelper.GroupReminder(s, s.Group?.Name ?? "");
            foreach (var member in s.Group?.Members ?? Enumerable.Empty<TrainingGroupMember>())
                await _push.SendToUserAsync(member.ClientId, title, body, data, cancellationToken);
            s.ReminderSentAt = now;
            sent++;
        }

        if (sent > 0) await _db.SaveChangesAsync(cancellationToken);
        return sent;
    }
}
