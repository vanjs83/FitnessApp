namespace FitnessApp.Application.Common.Interfaces;

/// <summary>
/// Provides the identity of the caller to CQRS handlers without exposing
/// HttpContext or ASP.NET Identity types to the Application layer.
/// </summary>
public interface ICurrentUserService
{
    string UserId { get; }
    bool IsInRole(string role);

    /// <summary>The caller's primary role from the JWT claims, or null.</summary>
    string? PrimaryRole { get; }

    /// <summary>Returns the TrainerId of the current user, or null if none/not a client.</summary>
    Task<string?> GetTrainerIdAsync(CancellationToken cancellationToken = default);
}
