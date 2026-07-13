using System.ComponentModel.DataAnnotations;

namespace FieldEvents.Agent.Models;

public sealed record AgentEventRequest
{
    [Required, MaxLength(200)]
    public string ExternalEventId { get; init; } = string.Empty;

    [Required, MaxLength(500)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Required, MaxLength(300)]
    public string Location { get; init; } = string.Empty;

    /// <summary>Case-insensitive. Valid values: Low, Medium, High, Critical.</summary>
    [Required]
    public string Priority { get; init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; init; }
}
