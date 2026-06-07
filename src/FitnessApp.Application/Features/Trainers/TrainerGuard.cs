using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;

namespace FitnessApp.Application.Features.Trainers;

internal static class TrainerGuard
{
    /// <summary>
    /// Verifies the given client exists and belongs to the trainer. Returns the error to
    /// surface (NotFound / Forbidden), or null when access is allowed.
    /// </summary>
    public static async Task<ResultError?> CheckOwnClientAsync(
        IUserDirectory users, string clientId, string trainerId, CancellationToken cancellationToken)
    {
        var client = await users.FindAsync(clientId, cancellationToken);
        if (client == null) return ResultError.NotFound;
        if (client.TrainerId != trainerId) return ResultError.Forbidden;
        return null;
    }
}
