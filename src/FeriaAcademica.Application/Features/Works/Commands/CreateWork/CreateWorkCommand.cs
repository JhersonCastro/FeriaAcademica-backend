using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.CreateWork;

/// <summary>The reviewer is not chosen: it is the professor whose invitation made the author a participant.</summary>
public sealed record CreateWorkCommand(Guid ParticipationId, string Title, string? Summary, Guid AuthorId) : ICommand<Guid>;
