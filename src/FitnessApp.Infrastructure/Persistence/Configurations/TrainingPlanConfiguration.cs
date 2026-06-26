using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class TrainingPlanConfiguration : IEntityTypeConfiguration<TrainingPlan>
{
    public void Configure(EntityTypeBuilder<TrainingPlan> e)
    {
        e.Property(x => x.Name).IsRequired().HasMaxLength(120);
        e.Property(x => x.TrainerId).IsRequired();
        e.Property(x => x.TrainerExpectations).HasMaxLength(2000);
        e.HasIndex(x => new { x.TrainerId, x.ClientId });
        e.HasIndex(x => x.ClientId);
        e.HasIndex(x => new { x.TrainerId, x.IsTemplate });

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
