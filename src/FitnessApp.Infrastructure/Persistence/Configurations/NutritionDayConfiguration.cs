using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class NutritionDayConfiguration : IEntityTypeConfiguration<NutritionDay>
{
    public void Configure(EntityTypeBuilder<NutritionDay> e)
    {
        e.Property(x => x.Label).IsRequired().HasMaxLength(60);
        e.Property(x => x.Notes).HasMaxLength(500);
        e.HasOne(x => x.NutritionPlan)
            .WithMany(p => p.Days)
            .HasForeignKey(x => x.NutritionPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        e.HasIndex(x => new { x.NutritionPlanId, x.DayOfWeek });
    }
}
