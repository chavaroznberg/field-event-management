using System.ComponentModel.DataAnnotations;

namespace FieldEvents.Application.DTOs;

public sealed record IngestEventRequest
{
    [Required, MaxLength(200)]
    public string ExternalEventId { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string SourceId { get; init; } = string.Empty;

    [Required, MaxLength(500)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Required, MaxLength(300)]
    public string Location { get; init; } = string.Empty;

    /// <summary>Case-insensitive. Valid values: Low, Medium, High, Critical.</summary>
    [Required]
    public string Priority { get; init; } = string.Empty;

    /// <summary>When the physical event occurred. Defaults to UtcNow if omitted.</summary>
    public DateTimeOffset OccurredAt { get; init; }
}
