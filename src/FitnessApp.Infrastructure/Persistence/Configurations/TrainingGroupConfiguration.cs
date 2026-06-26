using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class TrainingGroupConfiguration : IEntityTypeConfiguration<TrainingGroup>
{
    public void Configure(EntityTypeBuilder<TrainingGroup> e)
    {
        e.Property(x => x.TrainerId).IsRequired();
        e.Property(x => x.Name).IsRequired().HasMaxLength(120);
        e.HasIndex(x => new { x.TrainerId, x.IsActive });

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasMany(x => x.Members)
            .WithOne(m => m.Group!)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
