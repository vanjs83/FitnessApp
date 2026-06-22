using System.Linq.Expressions;
using FitnessApp.Application.DTOs.Progress;
using FitnessApp.Application.Storage;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Progress;

internal static class ProgressMapping
{
    /// <summary>
    /// Projection used in EF queries. <paramref name="mediaBase"/> (empty = same-origin) is
    /// prepended to the stored relative path so clients fetch images from the configured host/CDN.
    /// </summary>
    public static Expression<Func<ProgressPhoto, ProgressPhotoDto>> ToDto(string mediaBase) => p => new ProgressPhotoDto
    {
        Id = p.Id,
        PlanId = p.PlanId,
        ImageUrl = mediaBase + p.ImagePath,
        Pose = p.Pose,
        TakenOn = p.TakenOn,
        Note = p.Note,
        CreatedAt = p.CreatedAt
    };

    public static ProgressPhotoDto ToDtoObject(this ProgressPhoto p, string mediaBase) => new()
    {
        Id = p.Id,
        PlanId = p.PlanId,
        ImageUrl = StorageSettings.ToPublicUrl(mediaBase, p.ImagePath),
        Pose = p.Pose,
        TakenOn = p.TakenOn,
        Note = p.Note,
        CreatedAt = p.CreatedAt
    };
}
