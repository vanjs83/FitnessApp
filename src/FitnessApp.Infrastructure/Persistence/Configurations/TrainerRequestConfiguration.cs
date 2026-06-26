using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class TrainerRequestConfiguration : IEntityTypeConfiguration<TrainerRequest>
{
    public void Configure(EntityTypeBuilder<TrainerRequest> e)
    {
        e.Property(x => x.ClientId).IsRequired();
        e.Property(x => x.TrainerId).IsRequired();
        e.HasIndex(x => new { x.TrainerId, x.Status });
        e.HasIndex(x => new { x.ClientId, x.Status });

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.NoAction);
        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.TrainerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
