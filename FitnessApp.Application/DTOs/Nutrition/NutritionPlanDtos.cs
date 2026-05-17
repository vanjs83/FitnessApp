using System.ComponentModel.DataAnnotations;
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
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Range(0, 99999)]
    public decimal Price { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "EUR";
}

public class UpdateNutritionPlanRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Range(0, 99999)]
    public decimal Price { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "EUR";
}

public class CreateNutritionTemplateRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class CloneNutritionTemplateRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Range(0, 99999)]
    public decimal Price { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "EUR";
}

public class AddNutritionDayRequest
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required, MaxLength(60)]
    public string Label { get; set; } = string.Empty;

    [Range(0, 20000)]
    public int? TotalCaloriesTarget { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class AddMealRequest
{
    [Required]
    public MealType MealType { get; set; }

    public string? Time { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class AddMealItemRequest
{
    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? Quantity { get; set; }

    [Range(0, 20000)]
    public int? Calories { get; set; }

    [Range(0, 1000)]
    public decimal? ProteinG { get; set; }

    [Range(0, 1000)]
    public decimal? CarbsG { get; set; }

    [Range(0, 1000)]
    public decimal? FatG { get; set; }
}
