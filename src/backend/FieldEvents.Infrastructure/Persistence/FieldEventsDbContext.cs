using FieldEvents.Domain.Aggregates;
using FieldEvents.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Infrastructure.Persistence;

public sealed class FieldEventsDbContext : DbContext
{
    public FieldEventsDbContext(DbContextOptions<FieldEventsDbContext> options)
        : base(options) { }

    public DbSet<FieldEvent> FieldEvents => Set<FieldEvent>();
    public DbSet<SourceSystem> SourceSystems => Set<SourceSystem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EventStatusHistory> EventStatusHistory => Set<EventStatusHistory>();
    public DbSet<EventAssignment> EventAssignments => Set<EventAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FieldEventsDbContext).Assembly);

        // rowversion is SQL Server-specific; SQLite (used in integration tests) requires a
        // fallback mapping — byte[] stored as BLOB, no concurrency token.
        if (!Database.IsSqlServer())
        {
            modelBuilder.Entity<FieldEvent>()
                .Property(e => e.RowVersion)
                .IsConcurrencyToken(false)
                .ValueGeneratedNever()
                .HasColumnType("BLOB");
        }
    }
}
