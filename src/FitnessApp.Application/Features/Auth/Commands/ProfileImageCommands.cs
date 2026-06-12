using FitnessApp.Application.Common;
using FitnessApp.Application.Common.Interfaces;
using FitnessApp.Application.Interfaces;
using FitnessApp.Application.Storage;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FitnessApp.Application.Features.Auth.Commands;

public static class ProfileImageUpload
{
    public const long MaxImageBytes = 5 * 1024 * 1024;
    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public static FileUploadOptions Options(StorageSettings storage, string contentRoot) => new()
    {
        FolderPath = storage.ResolveProfileImagesPath(contentRoot),
        UrlPrefix = storage.ProfileImagesUrl,
        AllowedExtensions = AllowedImageExtensions,
        MaxBytes = MaxImageBytes
    };
}

// ===== Upload / replace own profile image =====

public record UploadProfileImageCommand(IFormFile File) : IRequest<Result<string>>;

public class UploadProfileImageCommandHandler : IRequestHandler<UploadProfileImageCommand, Result<string>>
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly StorageSettings _storage;
    private readonly IWebHostEnvironment _env;

    public UploadProfileImageCommandHandler(
        IAuthService auth, ICurrentUserService currentUser, IFileStorageService files,
        IOptions<StorageSettings> storage, IWebHostEnvironment env)
    {
        _auth = auth;
        _currentUser = currentUser;
        _files = files;
        _storage = storage.Value;
        _env = env;
    }

    public async Task<Result<string>> Handle(UploadProfileImageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var options = ProfileImageUpload.Options(_storage, _env.ContentRootPath);

        var saved = await _files.SaveAsync(request.File, options);
        if (!saved.Success)
            return Result<string>.Fail(ResultError.Validation, saved.ErrorMessage);

        var current = await _auth.GetProfileImagePathAsync(userId, cancellationToken);
        _files.DeleteByUrl(current, options.FolderPath, options.UrlPrefix);

        var (succeeded, errors) = await _auth.SetProfileImagePathAsync(userId, saved.Url, cancellationToken);
        if (!succeeded)
        {
            _files.DeleteByUrl(saved.Url, options.FolderPath, options.UrlPrefix);
            return Result<string>.Fail(ResultError.Validation, string.Join(", ", errors));
        }

        return Result<string>.Success(saved.Url!);
    }
}

// ===== Remove own profile image =====

public record DeleteProfileImageCommand : IRequest<Result>;

public class DeleteProfileImageCommandHandler : IRequestHandler<DeleteProfileImageCommand, Result>
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly StorageSettings _storage;
    private readonly IWebHostEnvironment _env;

    public DeleteProfileImageCommandHandler(
        IAuthService auth, ICurrentUserService currentUser, IFileStorageService files,
        IOptions<StorageSettings> storage, IWebHostEnvironment env)
    {
        _auth = auth;
        _currentUser = currentUser;
        _files = files;
        _storage = storage.Value;
        _env = env;
    }

    public async Task<Result> Handle(DeleteProfileImageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var options = ProfileImageUpload.Options(_storage, _env.ContentRootPath);

        var current = await _auth.GetProfileImagePathAsync(userId, cancellationToken);
        _files.DeleteByUrl(current, options.FolderPath, options.UrlPrefix);

        var (succeeded, errors) = await _auth.SetProfileImagePathAsync(userId, null, cancellationToken);
        return succeeded ? Result.Success() : Result.Fail(ResultError.Validation, string.Join(", ", errors));
    }
}
