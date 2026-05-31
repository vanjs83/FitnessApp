namespace FitnessApp.Application.Common.Interfaces;

public record UserInfo(string Id, string? FullName, string? Email, string? TrainerId)
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

    /// <summary>Maps the given user ids to their display name (FullName ?? Email).</summary>
    Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
        IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}
