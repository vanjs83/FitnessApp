namespace FitnessApp.Application.Storage;

public class StorageSettings
{
    /// <summary>
    /// Absolute base (CDN / external provider / prod host) prepended to stored relative media
    /// paths when serving them. Empty = same-origin relative URLs (default in production).
    /// Useful locally: point it at the prod host so images from the prod DB resolve while debugging.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "";

    public string ProfileImagesPath { get; set; } = "wwwroot/uploads/profiles";
    public string ProfileImagesUrl { get; set; } = "/uploads/profiles";
    public string ExerciseVideosPath { get; set; } = "wwwroot/uploads/exercises";
    public string ExerciseVideosUrl { get; set; } = "/uploads/exercises";
    public string ExerciseImagesPath { get; set; } = "wwwroot/uploads/exercise-images";
    public string ExerciseImagesUrl { get; set; } = "/uploads/exercise-images";
    public string ProgressImagesPath { get; set; } = "wwwroot/uploads/progress";
    public string ProgressImagesUrl { get; set; } = "/uploads/progress";

    public string ResolveProfileImagesPath(string contentRoot) => Resolve(contentRoot, ProfileImagesPath);
    public string ResolveExerciseVideosPath(string contentRoot) => Resolve(contentRoot, ExerciseVideosPath);
    public string ResolveExerciseImagesPath(string contentRoot) => Resolve(contentRoot, ExerciseImagesPath);
    public string ResolveProgressImagesPath(string contentRoot) => Resolve(contentRoot, ProgressImagesPath);

    private static string Resolve(string contentRoot, string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(contentRoot, path));

    /// <summary>PublicBaseUrl without a trailing slash; "" when unset.</summary>
    public string MediaBase =>
        string.IsNullOrWhiteSpace(PublicBaseUrl) ? "" : PublicBaseUrl.TrimEnd('/');

    /// <summary>
    /// Prepends <see cref="MediaBase"/> to a stored relative path. Leaves already-absolute
    /// (http...) paths untouched so future CDN-stored URLs pass through unchanged.
    /// </summary>
    public static string ToPublicUrl(string baseUrl, string? path) =>
        string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(path) ||
        path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? path ?? ""
            : baseUrl + path;
}
