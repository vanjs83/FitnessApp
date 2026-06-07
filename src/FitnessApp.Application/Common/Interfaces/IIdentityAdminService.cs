namespace FitnessApp.Application.Common.Interfaces;

public record AdminUserInfo(string Id, string Email, string? FullName, DateTime CreatedAt, string? TrainerId, string? ProfileImageUrl = null);

public enum TrainerDeleteOutcome { Deleted, NotFound, NotTrainer }

/// <summary>
/// Admin-level user/role operations backed by ASP.NET Identity, exposed to CQRS
/// handlers without leaking UserManager/ApplicationUser into the Application layer.
/// </summary>
public interface IIdentityAdminService
{
    Task<IReadOnlyList<AdminUserInfo>> GetUsersInRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<AdminUserInfo?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, AdminUserInfo? User, IReadOnlyList<string> Errors)> CreateTrainerAsync(
        string email, string? fullName, string password, CancellationToken cancellationToken = default);

    /// <summary>Soft-deactivates a trainer and unassigns their clients. </summary>
    Task<TrainerDeleteOutcome> DeactivateTrainerAsync(string id, CancellationToken cancellationToken = default);
}
