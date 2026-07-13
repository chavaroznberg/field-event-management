using FieldEvents.Domain.Aggregates;
using FieldEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldEvents.Infrastructure.Persistence.Configurations;

internal sealed class FieldEventConfiguration : IEntityTypeConfiguration<FieldEvent>
{
    public void Configure(EntityTypeBuilder<FieldEvent> builder)
    {
        builder.ToTable("FieldEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExternalEventId)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.SourceId)
               .IsRequired()
               .HasMaxLength(100);

        // Idempotency: same external system cannot report the same event ID twice.
        builder.HasIndex(e => new { e.SourceId, e.ExternalEventId })
               .IsUnique()
               .HasDatabaseName("UQ_FieldEvents_SourceId_ExternalEventId");

        builder.Property(e => e.Title)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.Description)
               .HasMaxLength(2000);

        builder.Property(e => e.Location)
               .IsRequired()
               .HasMaxLength(300);

        // Store enums as strings so the DB is human-readable and survives reordering.
        builder.Property(e => e.Priority)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // SQL Server rowversion — EF Core uses it for optimistic concurrency.
        builder.Property(e => e.RowVersion)
               .IsRowVersion();

        // FieldEvent._statusHistory is a private List<T> exposed only as IReadOnlyCollection<T>.
        // HasField + Field access mode tell EF Core to bypass the public property
        // and write directly to the backing field during materialisation.
        builder.HasMany(e => e.StatusHistory)
               .WithOne()
               .HasForeignKey(h => h.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.StatusHistory)
               .HasField("_statusHistory")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Assignments)
               .WithOne()
               .HasForeignKey(a => a.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Assignments)
               .HasField("_assignments")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
