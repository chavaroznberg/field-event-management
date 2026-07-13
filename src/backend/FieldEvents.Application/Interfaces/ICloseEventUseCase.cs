using FieldEvents.Application.Commands;
using FieldEvents.Application.DTOs;

namespace FieldEvents.Application.Interfaces;

public interface ICloseEventUseCase
{
    Task<IngestEventResponse> ExecuteAsync(CloseEventCommand command, CancellationToken ct = default);
}
