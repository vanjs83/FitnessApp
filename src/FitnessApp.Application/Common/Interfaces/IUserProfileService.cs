using FitnessApp.Application.DTOs.Auth;

namespace FitnessApp.Application.Common.Interfaces;

/// <summary>
/// Reads a user's full personal profile (ApplicationUser fields + primary role) into
/// a <see cref="PersonalProfileDto"/>, for handlers that must not touch Identity types.
/// </summary>
public interface IUserProfileService
{
    Task<PersonalProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
}
