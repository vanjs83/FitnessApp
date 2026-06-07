using System.Linq.Expressions;
using FitnessApp.Application.DTOs.Exercises;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Exercises;

internal static class ExerciseMapping
{
    /// <summary>Projection used in EF queries. CanEdit is resolved against the current user id.</summary>
    public static Expression<Func<Exercise, ExerciseDto>> ToDto(string currentUserId) => e => new ExerciseDto
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        VideoUrl = e.VideoUrl,
        ImageUrl = e.ImageUrl,
        MuscleGroup = e.MuscleGroup,
        Type = e.Type,
        CanEdit = e.CreatedByUserId == currentUserId
    };

    public static ExerciseDto ToDto(this Exercise e, string currentUserId) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        VideoUrl = e.VideoUrl,
        ImageUrl = e.ImageUrl,
        MuscleGroup = e.MuscleGroup,
        Type = e.Type,
        CanEdit = e.CreatedByUserId == currentUserId
    };

    public static string? NormalizeVideoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
