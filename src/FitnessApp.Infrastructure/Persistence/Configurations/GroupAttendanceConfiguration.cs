using FitnessApp.Domain.Entities;
using FitnessApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessApp.Infrastructure.Persistence.Configurations;

public class GroupAttendanceConfiguration : IEntityTypeConfiguration<GroupAttendance>
{
    public void Configure(EntityTypeBuilder<GroupAttendance> e)
    {
        e.Property(x => x.ClientId).IsRequired();

        // Cascade so cancelling/removing a group session clears its confirmations.
        e.HasOne(x => x.Appointment)
            .WithMany(a => a.Attendances)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // A client can confirm a given session at most once.
        e.HasIndex(x => new { x.AppointmentId, x.ClientId }).IsUnique();
    }
}
