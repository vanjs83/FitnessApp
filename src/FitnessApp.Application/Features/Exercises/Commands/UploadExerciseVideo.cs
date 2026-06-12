using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Exercises;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.Storage;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FitnessApp.Application.Features.Exercises.Commands;

public record UploadExerciseVideoCommand(int Id, IFormFile File) : IRequest<Result<ExerciseDto>>;

public class UploadExerciseVideoCommandHandler : IRequestHandler<UploadExerciseVideoCommand, Result<ExerciseDto>>
{
    public const long MaxVideoBytes = 100 * 1024 * 1024;
    private static readonly string[] AllowedVideoExtensions =
        { ".mp4", ".mov", ".webm", ".m4v", ".avi", ".mkv", ".3gp" };

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly StorageSettings _storage;
    private readonly IWebHostEnvironment _env;

    public UploadExerciseVideoCommandHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService files,
        IOptions<StorageSettings> storage,
        IWebHostEnvironment env)
    {
        _db = db;
        _currentUser = currentUser;
        _files = files;
        _storage = storage.Value;
        _env = env;
    }

    public async Task<Result<ExerciseDto>> Handle(UploadExerciseVideoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var e = await _db.Exercises.FindAsync(new object?[] { request.Id }, cancellationToken);
        if (e == null) return Result<ExerciseDto>.NotFound();
        if (e.CreatedByUserId != userId) return Result<ExerciseDto>.Forbidden();

        var options = new FileUploadOptions
        {
            FolderPath = _storage.ResolveExerciseVideosPath(_env.ContentRootPath),
            UrlPrefix = _storage.ExerciseVideosUrl,
            AllowedExtensions = AllowedVideoExtensions,
            MaxBytes = MaxVideoBytes
        };

        var result = await _files.SaveAsync(request.File, options);
        if (!result.Success)
            return Result<ExerciseDto>.Fail(ResultError.Validation, result.ErrorMessage);

        _files.DeleteByUrl(e.VideoUrl, options.FolderPath, options.UrlPrefix);
        e.VideoUrl = result.Url;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ExerciseDto>.Success(e.ToDto(userId));
    }
}
