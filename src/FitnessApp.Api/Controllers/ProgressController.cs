using System.Security.Claims;
using FitnessApp.Application.DTOs.Progress;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.Storage;
using FitnessApp.Domain.Common;
using FitnessApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FitnessApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Client)]
public class ProgressController : ControllerBase
{
    private readonly Infrastructure.Persistence.AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly StorageSettings _storage;
    private readonly IFileStorageService _files;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public ProgressController(
        Infrastructure.Persistence.AppDbContext db,
        IWebHostEnvironment env,
        IOptions<StorageSettings> storage,
        IFileStorageService files)
    {
        _db = db;
        _env = env;
        _storage = storage.Value;
        _files = files;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private FileUploadOptions ProgressImageOptions(string userId) => new()
    {
        FolderPath = _storage.ResolveProgressImagesPath(_env.ContentRootPath),
        UrlPrefix = _storage.ProgressImagesUrl,
        AllowedExtensions = AllowedImageExtensions,
        MaxBytes = MaxImageBytes,
        FileNamePrefix = userId
    };

    private static ProgressPhotoDto ToDto(ProgressPhoto p) => new()
    {
        Id = p.Id,
        ImageUrl = p.ImagePath,
        Pose = p.Pose,
        TakenOn = p.TakenOn,
        Note = p.Note,
        CreatedAt = p.CreatedAt
    };

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProgressPhotoDto>>> GetMine()
    {
        var photos = await _db.ProgressPhotos
            .Where(p => p.ClientId == UserId)
            .OrderByDescending(p => p.TakenOn)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Ok(photos);
    }

    [HttpPost]
    [RequestSizeLimit(MaxImageBytes + 1024)]
    public async Task<ActionResult<ProgressPhotoDto>> Upload(
        IFormFile file,
        [FromForm] ProgressPose pose,
        [FromForm] DateTime? takenOn,
        [FromForm] string? note)
    {
        var options = ProgressImageOptions(UserId);
        var saved = await _files.SaveAsync(file, options);
        if (!saved.Success)
            return BadRequest(new { message = saved.ErrorMessage });

        var photo = new ProgressPhoto
        {
            ClientId = UserId,
            ImagePath = saved.Url!,
            Pose = pose,
            TakenOn = takenOn ?? DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };

        _db.ProgressPhotos.Add(photo);
        await _db.SaveChangesAsync();

        return Ok(ToDto(photo));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var photo = await _db.ProgressPhotos.FirstOrDefaultAsync(p => p.Id == id && p.ClientId == UserId);
        if (photo == null) return NotFound();

        var options = ProgressImageOptions(UserId);
        _files.DeleteByUrl(photo.ImagePath, options.FolderPath, options.UrlPrefix);

        _db.ProgressPhotos.Remove(photo);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Photo removed." });
    }
}
