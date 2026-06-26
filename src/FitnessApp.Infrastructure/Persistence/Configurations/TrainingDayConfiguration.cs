using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class TrainingDayConfiguration : IEntityTypeConfiguration<TrainingDay>
{
    public void Configure(EntityTypeBuilder<TrainingDay> e)
    {
        e.Property(x => x.Label).IsRequired().HasMaxLength(60);
        e.Property(x => x.Notes).HasMaxLength(500);
        e.HasOne(x => x.TrainingPlan)
            .WithMany(p => p.Days)
            .HasForeignKey(x => x.TrainingPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        e.HasIndex(x => new { x.TrainingPlanId, x.DayOfWeek });
    }
}
