using FitnessApp.Application.Interfaces;
using FitnessApp.Application.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FitnessApp.Infrastructure.Storage;

public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(ILogger<FileStorageService> logger)
    {
        _logger = logger;
    }

    public async Task<FileUploadResult> SaveAsync(IFormFile? file, FileUploadOptions options)
    {
        if (file == null || file.Length == 0)
            return FileUploadResult.Fail("No file attached.");
        if (file.Length > options.MaxBytes)
            return FileUploadResult.Fail($"File is larger than {options.MaxBytes / (1024 * 1024)} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!options.AllowedExtensions.Contains(ext))
            return FileUploadResult.Fail($"Allowed formats: {string.Join(", ", options.AllowedExtensions)}.");

        Directory.CreateDirectory(options.FolderPath);

        var fileName = string.IsNullOrEmpty(options.FileNamePrefix)
            ? $"{Guid.NewGuid():N}{ext}"
            : $"{options.FileNamePrefix}_{Guid.NewGuid():N}{ext}";

        var fullPath = Path.Combine(options.FolderPath, fileName);
        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"{options.UrlPrefix.TrimEnd('/')}/{fileName}";
        return FileUploadResult.Ok(url);
    }

    public bool DeleteByUrl(string? url, string folderPath, string urlPrefix)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var prefix = urlPrefix.TrimEnd('/') + "/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

        var fileName = Path.GetFileName(url);
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        var fullPath = Path.Combine(folderPath, fileName);
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file {Path}. File may be in use; will be GC-cleaned later.", fullPath);
        }
        return false;
    }
}
