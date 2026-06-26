using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class MealItemConfiguration : IEntityTypeConfiguration<MealItem>
{
    public void Configure(EntityTypeBuilder<MealItem> e)
    {
        e.Property(x => x.Description).IsRequired().HasMaxLength(300);
        e.Property(x => x.Quantity).HasMaxLength(60);
        e.Property(x => x.ProteinG).HasColumnType("decimal(7,2)");
        e.Property(x => x.CarbsG).HasColumnType("decimal(7,2)");
        e.Property(x => x.FatG).HasColumnType("decimal(7,2)");
        e.HasOne(x => x.Meal)
            .WithMany(m => m.Items)
            .HasForeignKey(x => x.MealId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
