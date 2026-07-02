using FitnessApp.Application.Storage;
using FitnessApp.Infrastructure.Identity;
using FitnessApp.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    private readonly StorageSettings _storage;

    public ApplicationUserConfiguration(StorageSettings storage) => _storage = storage;

    public void Configure(EntityTypeBuilder<ApplicationUser> u)
    {
        u.HasOne(x => x.Trainer)
            .WithMany(x => x.Clients)
            .HasForeignKey(x => x.TrainerId)
            .OnDelete(DeleteBehavior.NoAction);
        u.HasIndex(x => x.TrainerId);
        u.HasQueryFilter(x => x.IsActive);
        u.Property(x => x.ProfileImagePath).HasConversion(ImageFileNameConverter.For(_storage.ProfileImagesUrl));
        // Explicit precision so weight isn't silently truncated to the default decimal(18,0).
        u.Property(x => x.WeightKg).HasPrecision(18, 2);
    }
}
