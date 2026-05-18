using System.Security.Claims;
using FitnessApp.Application.DTOs.Exercises;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly Infrastructure.Persistence.AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedVideoExtensions =
        { ".mp4", ".mov", ".webm", ".m4v", ".avi", ".mkv", ".3gp" };
    private const long MaxVideoBytes = 100 * 1024 * 1024;
    private const string VideoUploadsRelative = "/uploads/exercises/";

    public ExercisesController(Infrastructure.Persistence.AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetAll()
    {
        var userId = UserId;
        var trainerId = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TrainerId)
            .FirstOrDefaultAsync();

        var items = await _db.Exercises
            .Where(e => e.CreatedByUserId == userId
                || (trainerId != null && e.CreatedByUserId == trainerId))
            .OrderBy(e => e.Name)
            .Select(e => new ExerciseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                VideoUrl = e.VideoUrl,
                MuscleGroup = e.MuscleGroup,
                Type = e.Type,
                CanEdit = e.CreatedByUserId == userId
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> GetById(int id)
    {
        var e = await _db.Exercises.FindAsync(id);
        if (e == null) return NotFound();
        if (e.CreatedByUserId != UserId)
        {
            var trainerId = await _db.Users
                .Where(u => u.Id == UserId)
                .Select(u => u.TrainerId)
                .FirstOrDefaultAsync();
            if (e.CreatedByUserId != trainerId) return Forbid();
        }

        return Ok(MapDto(e));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Exercises.FindAsync(id);
        if (e == null) return NotFound();
        if (e.CreatedByUserId != UserId) return Forbid();

        var usedInPlan = await _db.PlannedExercises.AnyAsync(pe => pe.ExerciseId == id);
        if (usedInPlan)
            return BadRequest(new { message = "Exercise is in use in plans — remove it from them first." });

        _db.Exercises.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<ExerciseDto>> Create(CreateExerciseRequest request)
    {
        var entity = new Exercise
        {
            Name = request.Name,
            Description = request.Description,
            VideoUrl = NormalizeVideoUrl(request.VideoUrl),
            MuscleGroup = request.MuscleGroup,
            Type = request.Type,
            CreatedByUserId = UserId
        };

        _db.Exercises.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapDto(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> Update(int id, UpdateExerciseRequest request)
    {
        var e = await _db.Exercises.FindAsync(id);
        if (e == null) return NotFound();
        if (e.CreatedByUserId != UserId) return Forbid();

        e.Name = request.Name;
        e.Description = request.Description;
        e.VideoUrl = NormalizeVideoUrl(request.VideoUrl);
        e.MuscleGroup = request.MuscleGroup;
        e.Type = request.Type;

        await _db.SaveChangesAsync();
        return Ok(MapDto(e));
    }

    private ExerciseDto MapDto(Exercise e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        VideoUrl = e.VideoUrl,
        MuscleGroup = e.MuscleGroup,
        Type = e.Type,
        CanEdit = e.CreatedByUserId == UserId
    };

    private static string? NormalizeVideoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    [HttpPost("{id:int}/video")]
    [RequestSizeLimit(MaxVideoBytes + 1024)]
    public async Task<ActionResult<ExerciseDto>> UploadVideo(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No video attached." });
        if (file.Length > MaxVideoBytes)
            return BadRequest(new { message = $"Video is larger than {MaxVideoBytes / (1024 * 1024)} MB." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedVideoExtensions.Contains(ext))
            return BadRequest(new { message = $"Allowed formats: {string.Join(", ", AllowedVideoExtensions)}." });

        var e = await _db.Exercises.FindAsync(id);
        if (e == null) return NotFound();
        if (e.CreatedByUserId != UserId) return Forbid();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "exercises");
        Directory.CreateDirectory(folder);

        var fileName = $"{e.Id}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        DeleteExistingLocalVideo(e, webRoot);

        e.VideoUrl = $"{VideoUploadsRelative}{fileName}";
        await _db.SaveChangesAsync();

        return Ok(MapDto(e));
    }

    [HttpDelete("{id:int}/video")]
    public async Task<ActionResult<ExerciseDto>> DeleteVideo(int id)
    {
        var e = await _db.Exercises.FindAsync(id);
        if (e == null) return NotFound();
        if (e.CreatedByUserId != UserId) return Forbid();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        DeleteExistingLocalVideo(e, webRoot);

        e.VideoUrl = null;
        await _db.SaveChangesAsync();

        return Ok(MapDto(e));
    }

    private static void DeleteExistingLocalVideo(Exercise e, string webRoot)
    {
        if (string.IsNullOrWhiteSpace(e.VideoUrl)) return;
        if (!e.VideoUrl.StartsWith(VideoUploadsRelative, StringComparison.OrdinalIgnoreCase)) return;
        var relative = e.VideoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(webRoot, relative);
        try { if (System.IO.File.Exists(full)) System.IO.File.Delete(full); }
        catch { /* ignore — file might be in use, will be GC-cleaned later */ }
    }
}
