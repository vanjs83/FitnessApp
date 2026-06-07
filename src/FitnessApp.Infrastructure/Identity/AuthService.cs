using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Auth;
using FitnessApp.Infrastructure.Persistence;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FitnessApp.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly GoogleAuthSettings _googleAuth;
    private readonly AppDbContext _db;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IOptions<GoogleAuthSettings> googleAuth,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _googleAuth = googleAuth.Value;
        _db = db;
    }

    public string? GoogleClientId => _googleAuth.ClientId;

    private AuthResponse Issue(ApplicationUser user, string role)
    {
        var (token, expiresAt) = _tokenService.CreateToken(user.Id, user.Email!, role);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email!,
            FullName = user.FullName,
            Role = role
        };
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => await _userManager.FindByEmailAsync(email) != null;

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors, AuthResponse? Response)> RegisterAsync(
        string email, string? fullName, string password, string role, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, TrainerId = null };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description).ToList(), null);

        await _userManager.AddToRoleAsync(user, role);
        return (true, Array.Empty<string>(), Issue(user, role));
    }

    public async Task<(LoginResultCode Code, AuthResponse? Response)> LoginAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return (LoginResultCode.UserNotFound, null);

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!result.Succeeded) return (LoginResultCode.WrongPassword, null);

        var roles = await _userManager.GetRolesAsync(user);
        return (LoginResultCode.Success, Issue(user, roles.FirstOrDefault() ?? Roles.Client));
    }

    public async Task<(bool Ok, bool Unauthorized, string? Error, AuthResponse? Response)> GoogleLoginAsync(
        string credential, string? requestedRole, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_googleAuth.ClientId))
            return (false, false, "Google sign-in is not configured.", null);
        if (string.IsNullOrWhiteSpace(credential))
            return (false, false, "Missing Google credential.", null);

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(credential,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _googleAuth.ClientId } });
        }
        catch (InvalidJwtException)
        {
            return (false, true, "Invalid Google token.", null);
        }

        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
            return (false, true, "Google account email is not verified.", null);

        var user = await _userManager.FindByEmailAsync(payload.Email);
        if (user != null)
        {
            var existingRoles = await _userManager.GetRolesAsync(user);
            return (true, false, null, Issue(user, existingRoles.FirstOrDefault() ?? Roles.Client));
        }

        var role = Roles.SelfRegisterable.Contains(requestedRole) ? requestedRole! : Roles.Client;
        user = new ApplicationUser
        {
            UserName = payload.Email,
            Email = payload.Email,
            FullName = string.IsNullOrWhiteSpace(payload.Name) ? null : payload.Name,
            EmailConfirmed = true,
            TrainerId = null
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            return (false, false, string.Join(", ", result.Errors.Select(e => e.Description)), null);

        await _userManager.AddToRoleAsync(user, role);
        return (true, false, null, Issue(user, role));
    }

    public async Task<MeResponse?> GetMeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        string? trainerName = null;
        string? trainerImageUrl = null;
        if (user.TrainerId != null)
        {
            var trainer = await _db.Users.FirstOrDefaultAsync(u => u.Id == user.TrainerId, cancellationToken);
            trainerName = trainer?.FullName ?? trainer?.Email;
            trainerImageUrl = trainer?.ProfileImagePath;
        }

        return new MeResponse
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? Roles.Client,
            TrainerId = user.TrainerId,
            TrainerName = trainerName,
            TrainerImageUrl = trainerImageUrl,
            ProfileImageUrl = user.ProfileImagePath,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors, string? FullName)> UpdateFullNameAsync(
        string userId, string? fullName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, new[] { "User not found." }, null);

        user.FullName = fullName;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? (true, Array.Empty<string>(), user.FullName)
            : (false, result.Errors.Select(e => e.Description).ToList(), null);
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors)> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, new[] { "User not found." });

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded ? (true, Array.Empty<string>()) : (false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors)> DisconnectTrainerAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, new[] { "User not found." });

        user.TrainerId = null;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? (true, Array.Empty<string>()) : (false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors)> UpdatePersonalProfileAsync(
        string userId, UpdatePersonalProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, new[] { "User not found." });

        var isTrainer = await _userManager.IsInRoleAsync(user, Roles.Trainer);

        if (request.FullName != null) user.FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();
        user.BirthDate = request.BirthDate;
        user.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender;
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        if (isTrainer)
        {
            user.HeightCm = null;
            user.WeightKg = null;
            user.Goal = null;
            user.HealthNotes = null;
            user.ActivityLevel = null;
            user.PreferredWeeklyTrainingCount = null;
            user.PreferredTrainingType = null;
        }
        else
        {
            user.HeightCm = request.HeightCm;
            user.WeightKg = request.WeightKg;
            user.Goal = string.IsNullOrWhiteSpace(request.Goal) ? null : request.Goal.Trim();
            user.HealthNotes = string.IsNullOrWhiteSpace(request.HealthNotes) ? null : request.HealthNotes.Trim();
            user.ActivityLevel = string.IsNullOrWhiteSpace(request.ActivityLevel) ? null : request.ActivityLevel;
            user.PreferredWeeklyTrainingCount = request.PreferredWeeklyTrainingCount;
            user.PreferredTrainingType = request.PreferredTrainingType;
        }

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? (true, Array.Empty<string>()) : (false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<DeleteAccountResultCode> DeleteAccountAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return DeleteAccountResultCode.NotFound;

        if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin))
            return DeleteAccountResultCode.IsAdmin;

        if (await _userManager.IsInRoleAsync(user, Roles.Trainer))
        {
            var clients = await _db.Users.Where(u => u.TrainerId == user.Id).ToListAsync(cancellationToken);
            foreach (var c in clients)
                c.TrainerId = null;
        }

        user.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return DeleteAccountResultCode.Success;
    }

    public async Task<(string? Token, string? Email, string? FullName)> CreatePasswordResetTokenAsync(
        string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !await _userManager.HasPasswordAsync(user))
            return (null, null, null);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return (token, user.Email, user.FullName);
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors, string? Email, string? FullName)> ResetPasswordAsync(
        string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return (false, new[] { "The reset link is invalid or has expired." }, null, null);

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded
            ? (true, Array.Empty<string>(), user.Email, user.FullName)
            : (false, result.Errors.Select(e => e.Description).ToList(), null, null);
    }

    public async Task<string?> GetProfileImagePathAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.ProfileImagePath;
    }

    public async Task<(bool Succeeded, IReadOnlyList<string> Errors)> SetProfileImagePathAsync(
        string userId, string? path, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, new[] { "User not found." });

        user.ProfileImagePath = path;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? (true, Array.Empty<string>()) : (false, result.Errors.Select(e => e.Description).ToList());
    }
}
