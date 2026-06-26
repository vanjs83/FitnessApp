using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> e)
    {
        e.Property(x => x.TrainerId).IsRequired();
        e.Property(x => x.Location).HasMaxLength(400);
        e.Property(x => x.Notes).HasMaxLength(2000);
        e.HasIndex(x => new { x.TrainerId, x.StartsAt });
        e.HasIndex(x => new { x.ClientId, x.StartsAt });
        e.HasIndex(x => new { x.GroupId, x.StartsAt });

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);
        // Individual session → client (nullable for group sessions).
        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        // Group session → group (nullable for individual sessions).
        e.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
