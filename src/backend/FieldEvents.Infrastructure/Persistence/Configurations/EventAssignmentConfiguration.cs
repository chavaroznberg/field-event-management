using FieldEvents.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldEvents.Infrastructure.Persistence.Configurations;

internal sealed class EventAssignmentConfiguration : IEntityTypeConfiguration<EventAssignment>
{
    public void Configure(EntityTypeBuilder<EventAssignment> builder)
    {
        builder.ToTable("EventAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EventId).IsRequired();
        builder.Property(a => a.TechnicianId).IsRequired();
        builder.Property(a => a.AssignedByUserId).IsRequired();
        builder.Property(a => a.AssignedAt).IsRequired();

        // UnassignedAt is nullable — null means the technician is still assigned.
        builder.Property(a => a.UnassignedAt);

        builder.HasIndex(a => a.EventId)
               .HasDatabaseName("IX_EventAssignments_EventId");

        builder.HasIndex(a => a.TechnicianId)
               .HasDatabaseName("IX_EventAssignments_TechnicianId");
    }
}
