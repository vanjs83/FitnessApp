using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> e)
    {
        e.Property(x => x.UserId).IsRequired();
        e.Property(x => x.Token).IsRequired().HasMaxLength(512);
        e.Property(x => x.Platform).IsRequired().HasMaxLength(20);
        e.Property(x => x.UserAgent).HasMaxLength(500);
        e.HasIndex(x => x.Token).IsUnique();
        e.HasIndex(x => new { x.UserId, x.IsActive });

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
