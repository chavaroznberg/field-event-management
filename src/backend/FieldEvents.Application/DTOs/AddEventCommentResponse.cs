namespace FieldEvents.Application.DTOs;

public sealed record AddEventCommentResponse(
    Guid CommentId,
    Guid EventId,
    Guid AuthorId,
    string Text,
    DateTimeOffset CreatedAt);
