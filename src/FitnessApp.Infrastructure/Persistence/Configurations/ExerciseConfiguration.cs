using FitnessApp.Application.Storage;
using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    private readonly StorageSettings _storage;

    public ExerciseConfiguration(StorageSettings storage) => _storage = storage;

    public void Configure(EntityTypeBuilder<Exercise> e)
    {
        e.Property(x => x.Name).IsRequired().HasMaxLength(120);
        e.Property(x => x.MuscleGroup).HasMaxLength(60);
        e.Property(x => x.ImageUrl).HasConversion(ImageFileNameConverter.For(_storage.ExerciseImagesUrl));
        e.Property(x => x.VideoUrl).HasConversion(ImageFileNameConverter.For(_storage.ExerciseVideosUrl));
        e.HasIndex(x => x.Name);
    }
}
