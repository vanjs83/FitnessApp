using FitnessApp.Application.Storage;
using Microsoft.AspNetCore.Http;

namespace FitnessApp.Application.Interfaces;

public interface IFileStorageService
{
    Task<FileUploadResult> SaveAsync(IFormFile? file, FileUploadOptions options);
    bool DeleteByUrl(string? url, string folderPath, string urlPrefix);
}
