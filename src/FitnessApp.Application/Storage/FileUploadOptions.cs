namespace FitnessApp.Application.Storage;

public class FileUploadOptions
{
    public required string FolderPath { get; init; }
    public required string UrlPrefix { get; init; }
    public required IReadOnlyCollection<string> AllowedExtensions { get; init; }
    public required long MaxBytes { get; init; }
    public string FileNamePrefix { get; init; } = "";
}
