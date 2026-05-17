namespace FitnessApp.Domain.Entities;

public class MealItem
{
    public int Id { get; set; }
    public int MealId { get; set; }
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public int? Calories { get; set; }
    public decimal? ProteinG { get; set; }
    public decimal? CarbsG { get; set; }
    public decimal? FatG { get; set; }

    public Meal Meal { get; set; } = null!;
}
