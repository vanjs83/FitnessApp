using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> e)
    {
        e.Property(x => x.Notes).HasMaxLength(1000);
        e.HasOne(x => x.NutritionDay)
            .WithMany(d => d.Meals)
            .HasForeignKey(x => x.NutritionDayId)
            .OnDelete(DeleteBehavior.Cascade);
        e.HasIndex(x => new { x.NutritionDayId, x.Order });
    }
}
