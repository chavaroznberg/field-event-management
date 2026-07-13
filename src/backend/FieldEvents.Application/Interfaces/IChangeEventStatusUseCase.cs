using FieldEvents.Application.Commands;
using FieldEvents.Application.DTOs;

namespace FieldEvents.Application.Interfaces;

public interface IChangeEventStatusUseCase
{
    Task<IngestEventResponse> ExecuteAsync(ChangeEventStatusCommand command, CancellationToken ct = default);
}
