using FieldEvents.Application.DTOs;

namespace FieldEvents.Application.Interfaces;

public interface IGetRecentEventsUseCase
{
    Task<IReadOnlyList<IngestEventResponse>> ExecuteAsync(CancellationToken ct = default);
}
