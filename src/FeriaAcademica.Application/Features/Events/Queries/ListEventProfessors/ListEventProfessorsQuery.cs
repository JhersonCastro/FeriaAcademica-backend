using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.ListEventProfessors;

public sealed record ListEventProfessorsQuery(Guid EventId) : IQuery<IReadOnlyList<ProfessorDto>>;
