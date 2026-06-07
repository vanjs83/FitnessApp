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

public record UploadExerciseImageCommand(int Id, IFormFile File) : IRequest<Result<ExerciseDto>>;

public class UploadExerciseImageCommandHandler : IRequestHandler<UploadExerciseImageCommand, Result<ExerciseDto>>
{
    public const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageExtensions =
        { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly StorageSettings _storage;
    private readonly IWebHostEnvironment _env;

    public UploadExerciseImageCommandHandler(
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

    public async Task<Result<ExerciseDto>> Handle(UploadExerciseImageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var e = await _db.Exercises.FindAsync(new object?[] { request.Id }, cancellationToken);
        if (e == null) return Result<ExerciseDto>.NotFound();
        if (e.CreatedByUserId != userId) return Result<ExerciseDto>.Forbidden();

        var options = new FileUploadOptions
        {
            FolderPath = _storage.ResolveExerciseImagesPath(_env.ContentRootPath),
            UrlPrefix = _storage.ExerciseImagesUrl,
            AllowedExtensions = AllowedImageExtensions,
            MaxBytes = MaxImageBytes,
            FileNamePrefix = e.Id.ToString()
        };

        var result = await _files.SaveAsync(request.File, options);
        if (!result.Success)
            return Result<ExerciseDto>.Fail(ResultError.Validation, result.ErrorMessage);

        _files.DeleteByUrl(e.ImageUrl, options.FolderPath, options.UrlPrefix);
        e.ImageUrl = result.Url;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ExerciseDto>.Success(e.ToDto(userId));
    }
}
