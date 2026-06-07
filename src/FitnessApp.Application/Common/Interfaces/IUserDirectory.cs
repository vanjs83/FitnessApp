namespace FitnessApp.Application.Common.Interfaces;

public record UserInfo(string Id, string? FullName, string? Email, string? TrainerId, string? ProfileImageUrl = null, DateTime? CreatedAt = null)
{
    public string DisplayName => FullName ?? Email ?? "";
}

/// <summary>
/// Read-only access to user data for handlers, without exposing ASP.NET Identity's
/// ApplicationUser (which lives in Infrastructure) to the Application layer.
/// </summary>
public interface IUserDirectory
{
    Task<UserInfo?> FindAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Chat partners of the user: their clients (where TrainerId == userId) plus their own trainer.
    /// </summary>
    Task<IReadOnlyList<UserInfo>> GetLinkedPartnersAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>The clients assigned to the given trainer (ApplicationUser.TrainerId == trainerId).</summary>
    Task<IReadOnlyList<UserInfo>> GetClientsOfAsync(string trainerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if the two users are in a trainer&lt;-&gt;client relationship (either direction).
    /// </summary>
    Task<bool> AreLinkedAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);

    /// <summary>Maps the given user ids to their display name (FullName ?? Email).</summary>
    Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
        IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}
