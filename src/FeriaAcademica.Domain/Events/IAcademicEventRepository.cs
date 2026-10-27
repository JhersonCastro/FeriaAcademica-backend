namespace FeriaAcademica.Domain.Events;

public interface IAcademicEventRepository
{
    Task<AcademicEvent?> GetAsync(Guid id, CancellationToken ct);
    /// <summary>Loads the event (with its stages) that a participation belongs to.</summary>
    Task<AcademicEvent?> GetByParticipationAsync(Guid participationId, CancellationToken ct);
    Task AddAsync(AcademicEvent academicEvent, CancellationToken ct);
}
