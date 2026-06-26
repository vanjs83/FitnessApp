using FitnessApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class PlannedExerciseCompletionConfiguration : IEntityTypeConfiguration<PlannedExerciseCompletion>
{
    public void Configure(EntityTypeBuilder<PlannedExerciseCompletion> e)
    {
        e.Property(x => x.ClientId).IsRequired();
        e.HasIndex(x => new { x.PlannedExerciseId, x.CompletedAt });
        e.HasIndex(x => x.ClientId);

        e.HasOne(x => x.PlannedExercise)
            .WithMany(pe => pe.Completions)
            .HasForeignKey(x => x.PlannedExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
