using FitnessApp.Application.DTOs.Auth;

namespace FitnessApp.Application.Common.Interfaces;

public enum LoginResultCode { Success, UserNotFound, WrongPassword }
public enum DeleteAccountResultCode { Success, NotFound, IsAdmin }

/// <summary>
/// Authentication/account operations backed by ASP.NET Identity (UserManager, SignInManager,
/// token issuance, Google validation), exposed to CQRS handlers so the Application layer
/// never touches Identity types directly. Email sending and file IO stay in the handlers.
/// </summary>
public interface IAuthService
{
    /// <summary>Public Google client id for the SPA (empty when unconfigured).</summary>
    string? GoogleClientId { get; }

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IReadOnlyList<string> Errors, AuthResponse? Response)> RegisterAsync(
        string email, string? fullName, string password, string role, CancellationToken cancellationToken = default);

    Task<(LoginResultCode Code, AuthResponse? Response)> LoginAsync(
        string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Validates a Google credential and logs in or creates the account. Error is set on failure.</summary>
    Task<(bool Ok, bool Unauthorized, string? Error, AuthResponse? Response)> GoogleLoginAsync(
        string credential, string? requestedRole, CancellationToken cancellationToken = default);

    /// <summary>Validates a refresh token and rotates it, returning a fresh token pair. Ok is false when invalid/expired.</summary>
    Task<(bool Ok, AuthResponse? Response)> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes a refresh token (logout). No-op when the token is unknown.</summary>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<MeResponse?> GetMeAsync(string userId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IReadOnlyList<string> Errors, string? FullName)> UpdateFullNameAsync(
        string userId, string? fullName, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IReadOnlyList<string> Errors)> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IReadOnlyList<string> Errors)> DisconnectTrainerAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IReadOnlyList<string> Errors)> UpdatePersonalProfileAsync(
        string userId, UpdatePersonalProfileRequest request, CancellationToken cancellationToken = default);

    Task<DeleteAccountResultCode> DeleteAccountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Reset token + recipient info, or null token when no account / Google-only account.</summary>
    Task<(string? Token, string? Email, string? FullName)> CreatePasswordResetTokenAsync(
        string email, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IReadOnlyList<string> Errors, string? Email, string? FullName)> ResetPasswordAsync(
        string email, string token, string newPassword, CancellationToken cancellationToken = default);

    Task<string?> GetProfileImagePathAsync(string userId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IReadOnlyList<string> Errors)> SetProfileImagePathAsync(
        string userId, string? path, CancellationToken cancellationToken = default);
}
