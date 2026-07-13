using FieldEvents.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldEvents.Infrastructure.Persistence.Configurations;

internal sealed class SourceSystemConfiguration : IEntityTypeConfiguration<SourceSystem>
{
    public void Configure(EntityTypeBuilder<SourceSystem> builder)
    {
        builder.ToTable("SourceSystems");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.HasIndex(s => s.Name)
               .IsUnique()
               .HasDatabaseName("UQ_SourceSystems_Name");

        // SHA-256 hex = 64 chars; fixed length is a micro-optimisation but makes intent explicit.
        builder.Property(s => s.ApiKeyHash)
               .IsRequired()
               .HasMaxLength(64)
               .IsFixedLength();

        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
    }
}
