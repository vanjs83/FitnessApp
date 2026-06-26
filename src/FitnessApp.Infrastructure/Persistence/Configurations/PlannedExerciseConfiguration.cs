using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class PlannedExerciseConfiguration : IEntityTypeConfiguration<PlannedExercise>
{
    public void Configure(EntityTypeBuilder<PlannedExercise> e)
    {
        e.Property(x => x.TargetWeightKg).HasColumnType("decimal(7,2)");
        e.Property(x => x.Notes).HasMaxLength(500);
        e.HasOne(x => x.TrainingDay)
            .WithMany(d => d.Exercises)
            .HasForeignKey(x => x.TrainingDayId)
            .OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Exercise)
            .WithMany()
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
