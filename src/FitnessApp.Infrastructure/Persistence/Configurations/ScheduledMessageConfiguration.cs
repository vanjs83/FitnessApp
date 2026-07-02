using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class ScheduledMessageConfiguration : IEntityTypeConfiguration<ScheduledMessage>
{
    public void Configure(EntityTypeBuilder<ScheduledMessage> e)
    {
        e.Property(x => x.SenderId).IsRequired();
        e.Property(x => x.Subject).IsRequired().HasMaxLength(200);
        e.Property(x => x.Body).IsRequired().HasMaxLength(4000);
        e.Property(x => x.Error).HasMaxLength(2000);

        // The scan queries pending rows by due time — index it.
        e.HasIndex(x => new { x.Status, x.SendAtUtc });
        e.HasIndex(x => x.SenderId);

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
