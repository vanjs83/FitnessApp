using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Appointments;

internal static class AppointmentConflict
{
    /// <summary>
    /// True if the trainer already has a <see cref="AppointmentStatus.Scheduled"/> session
    /// overlapping the given slot. Only confirmed sessions block — tentative requests don't,
    /// so several clients can request the same free slot and the trainer picks one.
    /// </summary>
    public static async Task<bool> TrainerBusyAsync(
        IAppDbContext db, string trainerId, DateTime start, int durationMinutes, int? excludeId, CancellationToken cancellationToken)
    {
        var end = start.AddMinutes(durationMinutes);
        // Bound the query; max session is 8h, so a day back safely covers any session
        // whose end could spill into the new slot. Overlap is finished off in memory
        // because EndsAt is a computed property, not a column.
        var lowerBound = start.AddHours(-24);
        var exclude = excludeId ?? 0;

        var candidates = await db.Appointments
            .Where(a => a.TrainerId == trainerId
                        && a.Id != exclude
                        && a.Status == AppointmentStatus.Scheduled
                        && a.StartsAt < end
                        && a.StartsAt >= lowerBound)
            .ToListAsync(cancellationToken);

        return candidates.Any(a => start < a.EndsAt);
    }
}
