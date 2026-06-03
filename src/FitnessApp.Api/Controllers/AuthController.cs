using System.Security.Claims;
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
    private readonly IEmailService _email;
    private readonly ILogger<AuthController> _logger;

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
        IOptions<GoogleAuthSettings> googleAuth,
        IEmailService email,
        ILogger<AuthController> logger)
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
        _email = email;
        _logger = logger;
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

    // Step 1 of the forgot-password flow. Always returns 200 with the same body so an
    // attacker can't probe which emails have accounts. The reset email is sent only when
    // a matching account exists AND it has a password (Google-only accounts are skipped).
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var generic = Ok(new { message = "If an account exists for this email, a reset link has been sent." });

        if (string.IsNullOrWhiteSpace(request.Email))
            return generic;

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.HasPasswordAsync(user))
            return generic;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Reset link must target index.html explicitly: "/" serves landing.html (the
        // marketing page) as the default document, which has none of the auth logic.
        // auth.js reads the query params on load and shows the "set new password" form.
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var resetUrl = $"{baseUrl}/index.html?reset={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: user.Email!,
            subject: "FitnessApp – reset lozinke",
            templateKey: "password-reset",
            language: request.Language,
            placeholders: new Dictionary<string, string>
            {
                ["Name"] = user.FullName ?? user.Email!,
                ["ResetUrl"] = resetUrl
            });

        if (!ok)
            _logger.LogError("Failed to send password-reset email to {Email}: {Error}", user.Email, error);

        return generic;
    }

    // Step 2: consume the token and set the new password. A generic error is returned for
    // both "user not found" and "invalid/expired token" so neither can be distinguished.
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "The reset link is invalid or has expired." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BadRequest(new { message = "The reset link is invalid or has expired." });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // Confirmation mail is best-effort — a delivery failure must not fail the reset.
        var (ok, error) = await _email.SendTemplatedAsync(
            toEmail: user.Email!,
            subject: "FitnessApp – lozinka promijenjena",
            templateKey: "password-changed",
            language: request.Language,
            placeholders: new Dictionary<string, string>
            {
                ["Name"] = user.FullName ?? user.Email!
            });

        if (!ok)
            _logger.LogWarning("Password reset succeeded but confirmation email failed for {Email}: {Error}", user.Email, error);

        return Ok(new { message = "Password has been reset." });
    }

    // Self-service account deletion. Uses the same soft-delete approach as the admin
    // (IsActive=false): the global query filter then hides the account everywhere,
    // which also avoids the Restrict/NoAction FK constraints a hard delete would hit.
    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin))
            return BadRequest(new { message = "The administrator account cannot be deleted." });

        // If a trainer leaves, detach their clients so they aren't stuck pointing
        // at a trainer that no longer exists (mirrors AdminController.DeleteTrainer).
        if (await _userManager.IsInRoleAsync(user, Roles.Trainer))
        {
            var clients = await _db.Users.Where(u => u.TrainerId == user.Id).ToListAsync();
            foreach (var c in clients)
                c.TrainerId = null;
        }

        user.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
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
