using FieldEvents.Application.DTOs;
using FieldEvents.Application.Queries;

namespace FieldEvents.Application.Interfaces;

public interface IGetTechnicianEventsUseCase
{
    Task<IReadOnlyList<IngestEventResponse>> ExecuteAsync(
        GetTechnicianEventsQuery query, CancellationToken ct = default);
}
