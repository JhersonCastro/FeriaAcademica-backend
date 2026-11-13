namespace FeriaAcademica.Application.Features.Evaluation;

public interface IEvaluationReadModel
{
    /// <summary>Null when the event does not exist.</summary>
    Task<EventEvaluationDto?> GetEventEvaluationAsync(Guid eventId, CancellationToken ct);
    Task<IReadOnlyList<PendingEvaluationDto>> ListPendingAsync(Guid userId, CancellationToken ct);
    Task<MyWorkAnswersDto?> GetMyAnswersAsync(Guid eventId, Guid workId, Guid userId, CancellationToken ct);
}
