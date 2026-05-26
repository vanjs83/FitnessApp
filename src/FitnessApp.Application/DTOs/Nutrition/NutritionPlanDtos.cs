using FitnessApp.Domain.Entities;

namespace FitnessApp.Application.DTOs.Nutrition;

public class NutritionPlanListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DayCount { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public PaymentStatus PaymentStatus { get; set; }
}

public class NutritionTemplateListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int DayCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NutritionPlanDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsTemplate { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaymentClaimedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsLocked { get; set; }
    public List<NutritionDayDto> Days { get; set; } = new();
}

public class NutritionDayDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string Label { get; set; } = string.Empty;
    public int? TotalCaloriesTarget { get; set; }
    public string? Notes { get; set; }
    public List<MealDto> Meals { get; set; } = new();
}

public class MealDto
{
    public int Id { get; set; }
    public MealType MealType { get; set; }
    public string? Time { get; set; }
    public int Order { get; set; }
    public string? Notes { get; set; }
    public List<MealItemDto> Items { get; set; } = new();
}

public class MealItemDto
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public int? Calories { get; set; }
    public decimal? ProteinG { get; set; }
    public decimal? CarbsG { get; set; }
    public decimal? FatG { get; set; }
}

public class CreateNutritionPlanRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
}

public class UpdateNutritionPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
}

public class CreateNutritionTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CloneNutritionTemplateRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
}

public class AddNutritionDayRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public string Label { get; set; } = string.Empty;
    public int? TotalCaloriesTarget { get; set; }
    public string? Notes { get; set; }
}

public class AddMealRequest
{
    public MealType MealType { get; set; }
    public string? Time { get; set; }
    public string? Notes { get; set; }
}

public class AddMealItemRequest
{
    public string Description { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public int? Calories { get; set; }
    public decimal? ProteinG { get; set; }
    public decimal? CarbsG { get; set; }
    public decimal? FatG { get; set; }
}
