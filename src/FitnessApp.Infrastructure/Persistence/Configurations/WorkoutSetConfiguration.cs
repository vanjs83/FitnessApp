using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class WorkoutSetConfiguration : IEntityTypeConfiguration<WorkoutSet>
{
    public void Configure(EntityTypeBuilder<WorkoutSet> e)
    {
        e.Property(x => x.Weight).HasColumnType("decimal(7,2)");
        e.Property(x => x.Notes).HasMaxLength(500);
        e.HasOne(x => x.WorkoutExercise)
            .WithMany(we => we.Sets)
            .HasForeignKey(x => x.WorkoutExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
