using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.DTOs.Progress;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.Storage;
using FitnessApp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FitnessApp.Application.Features.Progress.Commands;

public record UploadProgressPhotoCommand(
    IFormFile File,
    ProgressPose Pose,
    DateTime? TakenOn,
    string? Note) : IRequest<Result<ProgressPhotoDto>>;

public class UploadProgressPhotoCommandHandler : IRequestHandler<UploadProgressPhotoCommand, Result<ProgressPhotoDto>>
{
    public const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly StorageSettings _storage;
    private readonly IWebHostEnvironment _env;

    public UploadProgressPhotoCommandHandler(
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

    public async Task<Result<ProgressPhotoDto>> Handle(UploadProgressPhotoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var options = new FileUploadOptions
        {
            FolderPath = _storage.ResolveProgressImagesPath(_env.ContentRootPath),
            UrlPrefix = _storage.ProgressImagesUrl,
            AllowedExtensions = AllowedImageExtensions,
            MaxBytes = MaxImageBytes
        };

        var saved = await _files.SaveAsync(request.File, options);
        if (!saved.Success)
            return Result<ProgressPhotoDto>.Fail(ResultError.Validation, saved.ErrorMessage);

        var photo = new ProgressPhoto
        {
            ClientId = userId,
            ImagePath = saved.Url!,
            Pose = request.Pose,
            TakenOn = request.TakenOn ?? DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        _db.ProgressPhotos.Add(photo);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ProgressPhotoDto>.Success(photo.ToDtoObject());
    }
}
