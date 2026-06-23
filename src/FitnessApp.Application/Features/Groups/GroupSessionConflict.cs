using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Groups;

internal static class GroupSessionConflict
{
    /// <summary>
    /// True if the trainer already has a <see cref="AppointmentStatus.Scheduled"/> group session
    /// overlapping the given slot. Mirrors AppointmentConflict; EndsAt is computed so overlap is
    /// finished in memory.
    /// </summary>
    public static async Task<bool> TrainerBusyAsync(
        IAppDbContext db, string trainerId, DateTime start, int durationMinutes, int? excludeId, CancellationToken cancellationToken)
    {
        var end = start.AddMinutes(durationMinutes);
        var lowerBound = start.AddHours(-24);
        var exclude = excludeId ?? 0;

        var candidates = await db.GroupSessions
            .Where(s => s.TrainerId == trainerId
                        && s.Id != exclude
                        && s.Status == AppointmentStatus.Scheduled
                        && s.StartsAt < end
                        && s.StartsAt >= lowerBound)
            .ToListAsync(cancellationToken);

        return candidates.Any(s => start < s.EndsAt);
    }
}
