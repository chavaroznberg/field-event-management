using FieldEvents.Agent.Queue;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Agent.Persistence;

public sealed class AgentDbContext : DbContext
{
    public AgentDbContext(DbContextOptions<AgentDbContext> options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var msg = modelBuilder.Entity<OutboxMessage>();

        msg.HasKey(m => m.Id);

        msg.Property(m => m.SourceId).IsRequired().HasMaxLength(100);
        msg.Property(m => m.ExternalEventId).IsRequired().HasMaxLength(200);
        msg.Property(m => m.IdempotencyKey).IsRequired().HasMaxLength(310);
        msg.Property(m => m.Payload).IsRequired();
        msg.Property(m => m.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        msg.Property(m => m.NextRetryAt).IsRequired();
        msg.Property(m => m.LastError).HasMaxLength(1000);

        // Unique constraint enforces agent-level deduplication (distinct from backend constraint).
        msg.HasIndex(m => m.IdempotencyKey)
           .IsUnique()
           .HasDatabaseName("UQ_OutboxMessages_IdempotencyKey");

        // Index speeds up the background worker's pending-message query.
        msg.HasIndex(m => new { m.Status, m.NextRetryAt })
           .HasDatabaseName("IX_OutboxMessages_Status_NextRetryAt");
    }
}
