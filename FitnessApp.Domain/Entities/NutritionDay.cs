namespace FitnessApp.Domain.Entities;

public class NutritionDay
{
    public int Id { get; set; }
    public int NutritionPlanId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string Label { get; set; } = string.Empty;
    public int? TotalCaloriesTarget { get; set; }
    public string? Notes { get; set; }

    public NutritionPlan NutritionPlan { get; set; } = null!;
    public ICollection<Meal> Meals { get; set; } = new List<Meal>();
}
