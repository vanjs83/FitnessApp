namespace FitnessApp.Domain.Entities;

public class Meal
{
    public int Id { get; set; }
    public int NutritionDayId { get; set; }
    public MealType MealType { get; set; }
    public TimeOnly? Time { get; set; }
    public int Order { get; set; }
    public string? Notes { get; set; }

    public NutritionDay NutritionDay { get; set; } = null!;
    public ICollection<MealItem> Items { get; set; } = new List<MealItem>();
}

public enum MealType
{
    Breakfast = 0,
    Snack1 = 1,
    Lunch = 2,
    Snack2 = 3,
    Dinner = 4,
    LateSnack = 5,
    Other = 99
}
