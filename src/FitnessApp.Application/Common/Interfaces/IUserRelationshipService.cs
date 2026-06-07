namespace FitnessApp.Application.Common.Interfaces;

public enum AssignTrainerOutcome { Assigned, ClientNotFound, ClientAlreadyHasTrainer }

/// <summary>
/// Trainer&lt;-&gt;client account operations that touch ASP.NET Identity (role checks and
/// the TrainerId link on ApplicationUser), exposed to CQRS handlers without leaking
/// UserManager/ApplicationUser into the Application layer.
/// </summary>
public interface IUserRelationshipService
{
    Task<bool> IsInRoleAsync(string userId, string role, CancellationToken cancellationToken = default);

    /// <summary>Assigns the client to the trainer, unless the client is missing or already linked.</summary>
    Task<AssignTrainerOutcome> AssignTrainerAsync(string clientId, string trainerId, CancellationToken cancellationToken = default);

    /// <summary>Creates a new client account (role Client) already linked to the given trainer.</summary>
    Task<(bool Succeeded, AdminUserInfo? User, IReadOnlyList<string> Errors)> CreateClientAsync(
        string email, string? fullName, string password, string trainerId, CancellationToken cancellationToken = default);
}
