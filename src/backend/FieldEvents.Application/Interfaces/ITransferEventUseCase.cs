using FieldEvents.Application.Commands;
using FieldEvents.Application.DTOs;

namespace FieldEvents.Application.Interfaces;

public interface ITransferEventUseCase
{
    Task<IngestEventResponse> ExecuteAsync(TransferEventCommand command, CancellationToken ct = default);
}
