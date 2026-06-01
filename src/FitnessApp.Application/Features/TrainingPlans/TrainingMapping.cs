using FitnessApp.Application.DTOs.TrainingPlans;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.TrainingPlans;

internal static class TrainingMapping
{
    public static PerformedSetDto MapPerformedSet(PerformedSet ps) => new()
    {
        Id = ps.Id,
        PlannedExerciseId = ps.PlannedExerciseId,
        SetNumber = ps.SetNumber,
        ActualReps = ps.ActualReps,
        ActualWeightKg = ps.ActualWeightKg,
        PerformedAt = ps.PerformedAt,
        Notes = ps.Notes
    };

    public static PlannedExerciseDto MapPlannedExercise(PlannedExercise e, string exerciseName)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return new PlannedExerciseDto
        {
            Id = e.Id,
            ExerciseId = e.ExerciseId,
            ExerciseName = exerciseName,
            Order = e.Order,
            TargetSets = e.TargetSets,
            TargetReps = e.TargetReps,
            TargetWeightKg = e.TargetWeightKg,
            TargetDurationSeconds = e.TargetDurationSeconds,
            RestSeconds = e.RestSeconds,
            Notes = e.Notes,
            CompletionsCount = e.Completions.Count,
            IsCompletedToday = e.Completions.Any(c => c.CompletedAt >= today && c.CompletedAt < tomorrow),
            LastCompletedAt = e.Completions.OrderByDescending(c => c.CompletedAt).FirstOrDefault()?.CompletedAt,
            RecentPerformedSets = e.PerformedSets
                .OrderByDescending(ps => ps.PerformedAt)
                .ThenByDescending(ps => ps.SetNumber)
                .Take(20)
                .Select(MapPerformedSet)
                .ToList()
        };
    }

    public static TrainingPlanDetailDto MapDetail(TrainingPlan p, string clientName, bool isLocked)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return new TrainingPlanDetailDto
        {
            Id = p.Id,
            Name = p.Name,
            ClientId = p.ClientId,
            ClientName = clientName,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            TrainerExpectations = isLocked ? null : p.TrainerExpectations,
            Price = p.Price,
            Currency = p.Currency,
            PaymentStatus = p.PaymentStatus,
            PaymentClaimedAt = p.PaymentClaimedAt,
            ApprovedAt = p.ApprovedAt,
            IsTemplate = p.IsTemplate,
            IsLocked = isLocked,
            Days = isLocked ? new() : p.Days
                .OrderBy(d => d.DayOfWeek)
                .Select(d => new TrainingDayDto
                {
                    Id = d.Id,
                    DayOfWeek = d.DayOfWeek,
                    Label = d.Label,
                    Notes = d.Notes,
                    IsCompletedToday = d.Exercises.Any() && d.Exercises.All(e =>
                        e.Completions.Any(c => c.CompletedAt >= today && c.CompletedAt < tomorrow)),
                    Exercises = d.Exercises
                        .OrderBy(e => e.Order)
                        .Select(e => MapPlannedExercise(e, e.Exercise?.Name ?? ""))
                        .ToList()
                }).ToList()
        };
    }

    public static string PdfFileName(TrainingPlan plan)
    {
        var safeName = string.Concat(plan.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "plan";
        return $"{safeName}_{plan.StartDate:yyyyMMdd}.pdf";
    }

    /// <summary>Deep-copies the days/exercises of <paramref name="source"/> into <paramref name="target"/>.</summary>
    public static void CopyDaysInto(TrainingPlan source, TrainingPlan target)
    {
        foreach (var d in source.Days.OrderBy(x => x.DayOfWeek))
        {
            var newDay = new TrainingDay
            {
                DayOfWeek = d.DayOfWeek,
                Label = d.Label,
                Notes = d.Notes
            };
            foreach (var pe in d.Exercises.OrderBy(x => x.Order))
            {
                newDay.Exercises.Add(new PlannedExercise
                {
                    ExerciseId = pe.ExerciseId,
                    Order = pe.Order,
                    TargetSets = pe.TargetSets,
                    TargetReps = pe.TargetReps,
                    TargetWeightKg = pe.TargetWeightKg,
                    TargetDurationSeconds = pe.TargetDurationSeconds,
                    RestSeconds = pe.RestSeconds,
                    Notes = pe.Notes
                });
            }
            target.Days.Add(newDay);
        }
    }
}
