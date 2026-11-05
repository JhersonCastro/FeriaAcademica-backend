namespace FeriaAcademica.Application.Features.Events.Queries.ListEventProfessors;

public interface IEventProfessorReadModel { Task<IReadOnlyList<ProfessorDto>> ListAsync(Guid eventId, CancellationToken ct); }
