namespace FitnessApp.Application.Storage;

public class StorageSettings
{
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
}
