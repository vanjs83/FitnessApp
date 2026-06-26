using FitnessApp.Application.Storage;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class ProgressPhotoConfiguration : IEntityTypeConfiguration<ProgressPhoto>
{
    private readonly StorageSettings _storage;

    public ProgressPhotoConfiguration(StorageSettings storage) => _storage = storage;

    public void Configure(EntityTypeBuilder<ProgressPhoto> e)
    {
        e.Property(x => x.ClientId).IsRequired();
        e.Property(x => x.ImagePath).IsRequired().HasMaxLength(400)
            .HasConversion(ImageFileNameConverter.For(_storage.ProgressImagesUrl));
        e.Property(x => x.Note).HasMaxLength(500);
        e.HasIndex(x => new { x.ClientId, x.Pose, x.TakenOn });
        e.HasIndex(x => x.PlanId);

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
        e.HasOne<TrainingPlan>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
