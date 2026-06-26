using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> e)
    {
        e.Property(x => x.SenderId).IsRequired();
        e.Property(x => x.RecipientId).IsRequired();
        e.Property(x => x.Body).IsRequired().HasMaxLength(2000);
        e.HasIndex(x => new { x.SenderId, x.RecipientId, x.SentAt });
        e.HasIndex(x => new { x.RecipientId, x.ReadAt });

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.NoAction);
        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.RecipientId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
