using System.Security.Claims;
using FitnessApp.Api.Data;
using FitnessApp.Application.DTOs.Auth;
using FitnessApp.Application.Interfaces;
using FitnessApp.Domain.Common;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        Infrastructure.Persistence.AppDbContext db,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _db = db;
        _env = env;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (!Roles.SelfRegisterable.Contains(request.Role))
            return BadRequest(new { message = $"Invalid role. Allowed: {string.Join(", ", Roles.SelfRegisterable)}." });

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { message = "A user with this email already exists." });

        string? trainerId = null;
        if (request.Role == Roles.Client && !string.IsNullOrWhiteSpace(request.TrainerId))
        {
            var trainer = await _userManager.FindByIdAsync(request.TrainerId);
            if (trainer == null || !await _userManager.IsInRoleAsync(trainer, Roles.Trainer))
                return BadRequest(new { message = "Selected trainer not found." });
            trainerId = trainer.Id;
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            TrainerId = trainerId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddToRoleAsync(user, request.Role);

        if (request.Role == Roles.Trainer)
        {
            await DbSeeder.SeedDefaultExercisesForTrainerAsync(_db, user.Id);
        }

        var (token, expiresAt) = _tokenService.CreateToken(user.Id, user.Email!, request.Role);
        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email!,
            FullName = user.FullName,
            Role = request.Role
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid email or password." });

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.Client;

        var (token, expiresAt) = _tokenService.CreateToken(user.Id, user.Email!, role);
        return Ok(new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email!,
            FullName = user.FullName,
            Role = role
        });
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

    [HttpPut("trainer")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> ChangeTrainer(ChangeTrainerRequest request)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.TrainerId))
        {
            user.TrainerId = null;
        }
        else
        {
            var trainer = await _userManager.FindByIdAsync(request.TrainerId);
            if (trainer == null || !await _userManager.IsInRoleAsync(trainer, Roles.Trainer))
                return BadRequest(new { message = "Selected trainer not found." });
            user.TrainerId = trainer.Id;
        }

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
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No image attached." });
        if (file.Length > MaxImageBytes)
            return BadRequest(new { message = "Image is larger than 5 MB." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            return BadRequest(new { message = "Dozvoljeni formati: JPG, PNG, WEBP." });

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "profiles");
        Directory.CreateDirectory(folder);

        var fileName = $"{user.Id}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        DeleteExistingProfileImage(user, webRoot);

        var relative = $"/uploads/profiles/{fileName}";
        user.ProfileImagePath = relative;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            System.IO.File.Delete(fullPath);
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        return Ok(new { profileImageUrl = relative });
    }

    [HttpDelete("profile-image")]
    [Authorize]
    public async Task<IActionResult> DeleteProfileImage()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        DeleteExistingProfileImage(user, webRoot);

        user.ProfileImagePath = null;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Image removed." });
    }

    private static void DeleteExistingProfileImage(ApplicationUser user, string webRoot)
    {
        if (string.IsNullOrWhiteSpace(user.ProfileImagePath)) return;
        var relative = user.ProfileImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(webRoot, relative);
        try { if (System.IO.File.Exists(full)) System.IO.File.Delete(full); }
        catch { /* ignore — best effort cleanup */ }
    }
}
