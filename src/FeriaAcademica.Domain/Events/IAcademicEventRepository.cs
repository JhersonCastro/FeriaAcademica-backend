namespace FeriaAcademica.Domain.Events;

public interface IAcademicEventRepository
{
    Task<AcademicEvent?> GetAsync(Guid id, CancellationToken ct);
    Task AddAsync(AcademicEvent academicEvent, CancellationToken ct);
}
