namespace FitnessApp.Application.Storage;

public class FileUploadResult
{
    public bool Success { get; init; }
    public string? Url { get; init; }
    public string? ErrorMessage { get; init; }

    public static FileUploadResult Ok(string url) => new() { Success = true, Url = url };
    public static FileUploadResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
