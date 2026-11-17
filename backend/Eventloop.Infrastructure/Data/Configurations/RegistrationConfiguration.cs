using Eventloop.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eventloop.Infrastructure.Data.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("Registrations");

        builder.HasKey(r => r.Id);

        // Unique constraint so a user cannot register for the same event twice
        builder.HasIndex(r => new { r.EventId, r.UserId })
            .IsUnique();

        // Unique ticket code index for rapid scanner lookup
        builder.HasIndex(r => r.TicketCode)
            .IsUnique();

        // Index on (EventId, Status) for capacity checks and attendee filtering
        builder.HasIndex(r => new { r.EventId, r.Status });

        // Foreign key indexes
        builder.HasIndex(r => r.EventId);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.CheckedInBy);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.WaitlistPosition)
            .IsRequired(false);

        builder.Property(r => r.TicketCode)
            .IsRequired();

        builder.Property(r => r.RegisteredAt)
            .IsRequired();

        builder.Property(r => r.CheckedInAt)
            .IsRequired(false);

        builder.Property(r => r.CheckedInBy)
            .IsRequired(false);

        builder.Property(r => r.ReminderSentAt)
            .IsRequired(false);
    }
}
