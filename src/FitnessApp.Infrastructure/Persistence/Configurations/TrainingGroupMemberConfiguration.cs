using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class TrainingGroupMemberConfiguration : IEntityTypeConfiguration<TrainingGroupMember>
{
    public void Configure(EntityTypeBuilder<TrainingGroupMember> e)
    {
        e.Property(x => x.ClientId).IsRequired();
        e.HasIndex(x => new { x.GroupId, x.ClientId }).IsUnique();
        e.HasIndex(x => x.ClientId);

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
