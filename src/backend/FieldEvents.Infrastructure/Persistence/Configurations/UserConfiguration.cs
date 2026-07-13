using FieldEvents.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldEvents.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(u => u.UserName)
               .IsUnique()
               .HasDatabaseName("UQ_Users_UserName");

        builder.Property(u => u.DisplayName)
               .IsRequired()
               .HasMaxLength(200);

        // bcrypt hash length is 60 chars; 200 gives headroom for future algorithm changes.
        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(u => u.Role)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(u => u.IsActive).IsRequired();
    }
}
