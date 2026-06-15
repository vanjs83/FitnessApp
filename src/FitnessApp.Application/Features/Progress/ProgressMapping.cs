using System.Linq.Expressions;
using FitnessApp.Application.DTOs.Progress;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Progress;

internal static class ProgressMapping
{
    /// <summary>Projection used in EF queries.</summary>
    public static Expression<Func<ProgressPhoto, ProgressPhotoDto>> ToDto => p => new ProgressPhotoDto
    {
        Id = p.Id,
        PlanId = p.PlanId,
        ImageUrl = p.ImagePath,
        Pose = p.Pose,
        TakenOn = p.TakenOn,
        Note = p.Note,
        CreatedAt = p.CreatedAt
    };

    public static ProgressPhotoDto ToDtoObject(this ProgressPhoto p) => new()
    {
        Id = p.Id,
        PlanId = p.PlanId,
        ImageUrl = p.ImagePath,
        Pose = p.Pose,
        TakenOn = p.TakenOn,
        Note = p.Note,
        CreatedAt = p.CreatedAt
    };
}
