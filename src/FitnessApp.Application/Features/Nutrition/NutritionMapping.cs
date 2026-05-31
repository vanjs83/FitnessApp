using FitnessApp.Application.DTOs.Nutrition;
using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.Features.Nutrition;

internal static class NutritionMapping
{
    public static NutritionPlanDetailDto MapDetail(NutritionPlan p, string clientName, bool isLocked) => new()
    {
        Id = p.Id,
        Name = p.Name,
        ClientId = p.ClientId,
        ClientName = clientName,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        Notes = isLocked ? null : p.Notes,
        IsTemplate = p.IsTemplate,
        Price = p.Price,
        Currency = p.Currency,
        PaymentStatus = p.PaymentStatus,
        PaymentClaimedAt = p.PaymentClaimedAt,
        ApprovedAt = p.ApprovedAt,
        IsLocked = isLocked,
        Days = isLocked ? new() : p.Days
            .OrderBy(d => d.DayOfWeek)
            .Select(d => new NutritionDayDto
            {
                Id = d.Id,
                DayOfWeek = d.DayOfWeek,
                Label = d.Label,
                TotalCaloriesTarget = d.TotalCaloriesTarget,
                Notes = d.Notes,
                Meals = d.Meals.OrderBy(m => m.Order).Select(m => new MealDto
                {
                    Id = m.Id,
                    MealType = m.MealType,
                    Time = m.Time?.ToString("HH:mm"),
                    Order = m.Order,
                    Notes = m.Notes,
                    Items = m.Items.OrderBy(i => i.Order).Select(i => new MealItemDto
                    {
                        Id = i.Id,
                        Order = i.Order,
                        Description = i.Description,
                        Quantity = i.Quantity,
                        Calories = i.Calories,
                        ProteinG = i.ProteinG,
                        CarbsG = i.CarbsG,
                        FatG = i.FatG
                    }).ToList()
                }).ToList()
            }).ToList()
    };

    public static string PdfFileName(NutritionPlan p)
    {
        var safeName = string.Concat(p.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "prehrana";
        return $"prehrana_{safeName}_{p.StartDate:yyyyMMdd}.pdf";
    }

    /// <summary>Deep-copies the days/meals/items of <paramref name="source"/> into <paramref name="target"/>.</summary>
    public static void CopyDaysInto(NutritionPlan source, NutritionPlan target)
    {
        foreach (var d in source.Days.OrderBy(x => x.DayOfWeek))
        {
            var nd = new NutritionDay
            {
                DayOfWeek = d.DayOfWeek,
                Label = d.Label,
                TotalCaloriesTarget = d.TotalCaloriesTarget,
                Notes = d.Notes
            };
            foreach (var m in d.Meals.OrderBy(x => x.Order))
            {
                var nm = new Meal
                {
                    MealType = m.MealType,
                    Time = m.Time,
                    Order = m.Order,
                    Notes = m.Notes
                };
                foreach (var it in m.Items.OrderBy(x => x.Order))
                {
                    nm.Items.Add(new MealItem
                    {
                        Order = it.Order,
                        Description = it.Description,
                        Quantity = it.Quantity,
                        Calories = it.Calories,
                        ProteinG = it.ProteinG,
                        CarbsG = it.CarbsG,
                        FatG = it.FatG
                    });
                }
                nd.Meals.Add(nm);
            }
            target.Days.Add(nd);
        }
    }
}
