using System.Security.Claims;
using FitnessApp.Api.Services;
using FitnessApp.Application.DTOs.Exercises;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ExercisesController : ControllerBase
{
    private readonly Infrastructure.Persistence.AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly StorageSettings _storage;
    private readonly FileStorageService _files;

    private static readonly string[] AllowedVideoExtensions =
        { ".mp4", ".mov", ".webm", ".m4v", ".avi", ".mkv", ".3gp" };
    private const long MaxVideoBytes = 100 * 1024 * 1024;

    public ExercisesController(
        Infrastructure.Persistence.AppDbContext db,
        IWebHostEnvironment env,
        IOptions<StorageSettings> storage,
        FileStorageService files)
    {
        _db = db;
        _env = env;
        _storage = storage.Value;
        _files = files;
    }

    private FileUploadOptions VideoUploadOptions(int exerciseId) => new()
    {
        FolderPath = _storage.ResolveExerciseVideosPath(_env.ContentRootPath),
        UrlPrefix = _storage.ExerciseVideosUrl,
        AllowedExtensions = AllowedVideoExtensions,
        MaxBytes = MaxVideoBytes,
        FileNamePrefix = exerciseId.ToString()
    };

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
        var e = await _db.Exercises.FindAsync(id);
        if (e == null) return NotFound();
        if (e.CreatedByUserId != UserId) return Forbid();

        var options = VideoUploadOptions(e.Id);
        var result = await _files.SaveAsync(file, options);
        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        _files.DeleteByUrl(e.VideoUrl, options.FolderPath, options.UrlPrefix);
        e.VideoUrl = result.Url;
        await _db.SaveChangesAsync();

        return Ok(MapDto(e));
    }

    [HttpDelete("{id:int}/video")]
    public async Task<ActionResult<ExerciseDto>> DeleteVideo(int id)
    {
        var e = await _db.Exercises.FindAsync(id);
        if (e == null) return NotFound();
        if (e.CreatedByUserId != UserId) return Forbid();

        var options = VideoUploadOptions(e.Id);
        _files.DeleteByUrl(e.VideoUrl, options.FolderPath, options.UrlPrefix);
        e.VideoUrl = null;
        await _db.SaveChangesAsync();

        return Ok(MapDto(e));
    }
}
