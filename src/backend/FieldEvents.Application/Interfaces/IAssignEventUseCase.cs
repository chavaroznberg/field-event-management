using FieldEvents.Application.Commands;
using FieldEvents.Application.DTOs;

namespace FieldEvents.Application.Interfaces;

public interface IAssignEventUseCase
{
    Task<IngestEventResponse> ExecuteAsync(AssignEventCommand command, CancellationToken ct = default);
}
