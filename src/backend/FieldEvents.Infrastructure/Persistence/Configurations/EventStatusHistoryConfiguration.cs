using FieldEvents.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldEvents.Infrastructure.Persistence.Configurations;

internal sealed class EventStatusHistoryConfiguration : IEntityTypeConfiguration<EventStatusHistory>
{
    public void Configure(EntityTypeBuilder<EventStatusHistory> builder)
    {
        builder.ToTable("EventStatusHistory");

        builder.HasKey(h => h.Id);

        // EventId FK is configured from the FieldEvent side (HasMany → WithOne).
        builder.Property(h => h.EventId).IsRequired();

        builder.Property(h => h.PreviousStatus)
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(h => h.NewStatus)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(h => h.ChangedByUserId).IsRequired();
        builder.Property(h => h.ChangedAt).IsRequired();

        builder.Property(h => h.Comment)
               .HasMaxLength(1000);

        builder.HasIndex(h => h.EventId)
               .HasDatabaseName("IX_EventStatusHistory_EventId");
    }
}
