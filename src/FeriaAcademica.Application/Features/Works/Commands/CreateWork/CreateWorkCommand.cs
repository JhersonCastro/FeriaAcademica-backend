using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.CreateWork;

public sealed record CreateWorkCommand(Guid ParticipationId, Guid ProfessorId, string Title, string? Summary, Guid AuthorId) : ICommand<Guid>;
