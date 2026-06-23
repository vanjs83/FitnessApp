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
/// <para>
/// StartsAt is stored as the user's naive wall-clock time, so "now" must be evaluated in that same
/// frame: <paramref name="TimeZoneId"/> (e.g. "Central European Standard Time") converts UtcNow to
/// the app's local time. Null = compare against UtcNow (used by tests, which store UTC StartsAt).
/// </para>
/// </summary>
public record SendDueAppointmentRemindersCommand(int LeadMinutes, string? TimeZoneId = null) : IRequest<int>;

public class SendDueAppointmentRemindersCommandHandler : IRequestHandler<SendDueAppointmentRemindersCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly IPushNotificationService _push;
    private readonly IUserDirectory _users;

    public SendDueAppointmentRemindersCommandHandler(IAppDbContext db, IPushNotificationService push, IUserDirectory users)
    {
        _db = db;
        _push = push;
        _users = users;
    }

    public async Task<int> Handle(SendDueAppointmentRemindersCommand request, CancellationToken cancellationToken)
    {
        var now = LocalNow(request.TimeZoneId);
        var windowEnd = now.AddMinutes(request.LeadMinutes);
        var sent = 0;

        // One query over all due sessions — individual and group are both appointments now.
        var appointments = await _db.Appointments
            .Include(a => a.Group!).ThenInclude(g => g.Members)
            .Where(a => a.Status == AppointmentStatus.Scheduled
                        && a.ReminderSentAt == null
                        && a.StartsAt > now && a.StartsAt <= windowEnd)
            .ToListAsync(cancellationToken);

        foreach (var a in appointments)
        {
            if (a.IsGroup)
            {
                var members = a.Group?.Members ?? Enumerable.Empty<TrainingGroupMember>();
                var memberIds = members.Select(m => m.ClientId).ToList();
                var names = await _users.GetDisplayNamesAsync(memberIds, cancellationToken);
                var memberNames = memberIds.Select(id => names.TryGetValue(id, out var n) ? n : "").ToList();
                var (gtitle, gbody, gdata) = AppointmentHelper.GroupReminder(a, a.Group?.Name ?? "", memberNames);
                foreach (var member in members)
                    await _push.SendToUserAsync(member.ClientId, gtitle, gbody, gdata, cancellationToken);
            }
            else if (a.ClientId is not null)
            {
                var (title, body, data) = AppointmentHelper.Reminder(a);
                await _push.SendToUserAsync(a.ClientId, title, body, data, cancellationToken);
            }
            a.ReminderSentAt = now;
            sent++;
        }

        if (sent > 0) await _db.SaveChangesAsync(cancellationToken);
        return sent;
    }

    // UtcNow converted to the app's wall-clock timezone (StartsAt is stored as naive local time).
    private static DateTime LocalNow(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return DateTime.UtcNow;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }
}
