using System.ComponentModel.DataAnnotations;

namespace FieldEvents.Application.Commands;

public sealed record AddEventCommentCommand(
    Guid EventId,
    Guid AuthorId,
    [MaxLength(2000)] string Text);
