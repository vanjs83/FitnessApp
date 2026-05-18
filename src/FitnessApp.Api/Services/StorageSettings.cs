namespace FitnessApp.Api.Services;

public class StorageSettings
{
    public string ProfileImagesPath { get; set; } = "wwwroot/uploads/profiles";
    public string ProfileImagesUrl { get; set; } = "/uploads/profiles";
    public string ExerciseVideosPath { get; set; } = "wwwroot/uploads/exercises";
    public string ExerciseVideosUrl { get; set; } = "/uploads/exercises";

    public string ResolveProfileImagesPath(string contentRoot) => Resolve(contentRoot, ProfileImagesPath);
    public string ResolveExerciseVideosPath(string contentRoot) => Resolve(contentRoot, ExerciseVideosPath);

    private static string Resolve(string contentRoot, string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(contentRoot, path));
}
