using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class WorkoutConfiguration : IEntityTypeConfiguration<Workout>
{
    public void Configure(EntityTypeBuilder<Workout> e)
    {
        e.Property(x => x.Name).IsRequired().HasMaxLength(120);
        e.Property(x => x.ClientId).IsRequired();
        e.Property(x => x.TrainerId).IsRequired();
        e.Property(x => x.Notes).HasMaxLength(2000);
        e.HasIndex(x => new { x.ClientId, x.PerformedAt });

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
