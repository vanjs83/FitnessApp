namespace FitnessApp.Domain.Entities;

/// <summary>
/// A long-lived, opaque refresh token. Only the SHA-256 hash of the token is stored, so a
/// database leak does not expose usable tokens. Tokens rotate on every use and are deleted
/// on rotation/logout — a missing row means the token is no longer valid.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>SHA-256 hash (Base64) of the raw token. The raw token is never persisted.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
