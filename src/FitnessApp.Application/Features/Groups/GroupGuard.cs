using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Application.Features.Groups;

internal static class GroupGuard
{
    /// <summary>
    /// Loads the group (with members) and verifies it belongs to the trainer.
    /// Returns the error to surface (NotFound / Forbidden), or the group when access is allowed.
    /// </summary>
    public static async Task<(TrainingGroup? Group, ResultError? Error)> LoadOwnedAsync(
        IAppDbContext db, int groupId, string trainerId, CancellationToken cancellationToken)
    {
        var group = await db.TrainingGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.IsActive, cancellationToken);

        if (group is null) return (null, ResultError.NotFound);
        if (group.TrainerId != trainerId) return (null, ResultError.Forbidden);
        return (group, null);
    }
}
