using System.Security.Claims;
using FitnessApp.Api.Data;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.Storage;
using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Auth;
using FitnessApp.Infrastructure.Identity;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly Infrastructure.Persistence.AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly StorageSettings _storage;
    private readonly IFileStorageService _files;
    private readonly GoogleAuthSettings _googleAuth;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        Infrastructure.Persistence.AppDbContext db,
        IWebHostEnvironment env,
        IOptions<StorageSettings> storage,
        IFileStorageService files,
        IOptions<GoogleAuthSettings> googleAuth)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _db = db;
        _env = env;
        _storage = storage.Value;
        _files = files;
        _googleAuth = googleAuth.Value;
    }

    private FileUploadOptions ProfileImageOptions(string userId) => new()
    {
        FolderPath = _storage.ResolveProfileImagesPath(_env.ContentRootPath),
        UrlPrefix = _storage.ProfileImagesUrl,
        AllowedExtensions = AllowedImageExtensions,
        MaxBytes = MaxImageBytes,
        FileNamePrefix = userId
    };

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (!Roles.SelfRegisterable.Contains(request.Role))
            return BadRequest(new { message = $"Invalid role. Allowed: {string.Join(", ", Roles.SelfRegisterable)}." });

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = "A user with this email already exists." });

        // Clients no longer pick a trainer at registration — they send a request
        // from their profile afterwards, which the trainer must accept.
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            TrainerId = null
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, request.Role);

        if (request.Role == Roles.Trainer)
        {
            await DbSeeder.SeedDefaultExercisesForTrainerAsync(_db, user.Id);
        }

        return Ok(Issue(user, request.Role));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { message = "No account found with this email address.", code = "user_not_found" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Incorrect password.", code = "wrong_password" });

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.Client;

        return Ok(Issue(user, role));
    }

    // ===== Google Sign-In =====

    // Public Google client ID so the SPA can initialise Google Identity Services
    // without hard-coding it in the frontend. Empty when the feature is unconfigured.
    [HttpGet("google-config")]
    public ActionResult<object> GoogleConfig() => Ok(new { clientId = _googleAuth.ClientId });

    // One endpoint covers both registration and login: if the verified Google email
    // already maps to a user we log them in; otherwise we create the account.
    // The desired role is honoured only when creating a new account (Register tab).
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(_googleAuth.ClientId))
            return BadRequest(new { message = "Google sign-in is not configured." });
        if (string.IsNullOrWhiteSpace(request.Credential))
            return BadRequest(new { message = "Missing Google credential." });

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.Credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleAuth.ClientId }
                });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { message = "Invalid Google token." });
        }

        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
            return Unauthorized(new { message = "Google account email is not verified." });

        var user = await _userManager.FindByEmailAsync(payload.Email);

        // Existing account: log in with the role already stored, ignoring the request.
        if (user != null)
        {
            var existingRoles = await _userManager.GetRolesAsync(user);
            return Ok(Issue(user, existingRoles.FirstOrDefault() ?? Roles.Client));
        }

        // New account: honour the requested role (Register tab), default to Client.
        var role = Roles.SelfRegisterable.Contains(request.Role) ? request.Role! : Roles.Client;

        user = new ApplicationUser
        {
            UserName = payload.Email,
            Email = payload.Email,
            FullName = string.IsNullOrWhiteSpace(payload.Name) ? null : payload.Name,
            EmailConfirmed = true,
            TrainerId = null
        };

        // No password: this account signs in exclusively via Google until/unless
        // the user sets a password later.
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, role);

        if (role == Roles.Trainer)
            await DbSeeder.SeedDefaultExercisesForTrainerAsync(_db, user.Id);

        return Ok(Issue(user, role));
    }

    // Builds the app's own JWT + response for any successful auth path
    // (password register/login or Google sign-in).
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

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        string? trainerName = null;
        string? trainerImageUrl = null;
        if (user.TrainerId != null)
        {
            var trainer = await _db.Users.FirstOrDefaultAsync(u => u.Id == user.TrainerId);
            trainerName = trainer?.FullName ?? trainer?.Email;
            trainerImageUrl = trainer?.ProfileImagePath;
        }

        return Ok(new MeResponse
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
        });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        user.FullName = request.FullName;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { fullName = user.FullName });
    }

    // Clients can only DISCONNECT from their trainer here. Connecting to a trainer
    // goes exclusively through a trainer request that the trainer must accept
    // (see TrainerRequestsController), so a non-null trainerId is rejected.
    [HttpPut("trainer")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> ChangeTrainer(ChangeTrainerRequest request)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.TrainerId))
            return BadRequest(new { message = "To connect to a trainer, send a request the trainer must accept." });

        user.TrainerId = null;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { trainerId = user.TrainerId });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Password changed successfully." });
    }

    [HttpGet("personal-profile")]
    [Authorize]
    public async Task<ActionResult<PersonalProfileDto>> GetPersonalProfile()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new PersonalProfileDto
        {
            FullName = user.FullName,
            Email = user.Email,
            BirthDate = user.BirthDate,
            Gender = user.Gender,
            HeightCm = user.HeightCm,
            WeightKg = user.WeightKg,
            Goal = user.Goal,
            HealthNotes = user.HealthNotes,
            ActivityLevel = user.ActivityLevel,
            Phone = user.Phone,
            PreferredWeeklyTrainingCount = user.PreferredWeeklyTrainingCount,
            PreferredTrainingType = user.PreferredTrainingType,
            ProfileImageUrl = user.ProfileImagePath,
            Role = roles.FirstOrDefault() ?? Roles.Client
        });
    }

    [HttpPut("personal-profile")]
    [Authorize]
    public async Task<IActionResult> UpdatePersonalProfile(UpdatePersonalProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

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
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Profile saved." });
    }

    [HttpPost("profile-image")]
    [Authorize]
    [RequestSizeLimit(MaxImageBytes + 1024)]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        var options = ProfileImageOptions(user.Id);
        var saved = await _files.SaveAsync(file, options);
        if (!saved.Success)
            return BadRequest(new { message = saved.ErrorMessage });

        _files.DeleteByUrl(user.ProfileImagePath, options.FolderPath, options.UrlPrefix);
        user.ProfileImagePath = saved.Url;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _files.DeleteByUrl(saved.Url, options.FolderPath, options.UrlPrefix);
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        return Ok(new { profileImageUrl = saved.Url });
    }

    [HttpDelete("profile-image")]
    [Authorize]
    public async Task<IActionResult> DeleteProfileImage()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        var options = ProfileImageOptions(user.Id);
        _files.DeleteByUrl(user.ProfileImagePath, options.FolderPath, options.UrlPrefix);
        user.ProfileImagePath = null;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Image removed." });
    }
}
