using FieldEvents.Application.Commands;
using FieldEvents.Application.DTOs;

namespace FieldEvents.Application.Interfaces;

public interface IAddEventCommentUseCase
{
    Task<AddEventCommentResponse> ExecuteAsync(AddEventCommentCommand command, CancellationToken ct = default);
}
