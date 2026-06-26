using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class PerformedSetConfiguration : IEntityTypeConfiguration<PerformedSet>
{
    public void Configure(EntityTypeBuilder<PerformedSet> e)
    {
        e.Property(x => x.ActualWeightKg).HasColumnType("decimal(7,2)");
        e.Property(x => x.Notes).HasMaxLength(500);
        e.HasIndex(x => new { x.PlannedExerciseId, x.PerformedAt });

        e.HasOne(x => x.PlannedExercise)
            .WithMany(pe => pe.PerformedSets)
            .HasForeignKey(x => x.PlannedExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
