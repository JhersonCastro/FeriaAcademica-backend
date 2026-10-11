using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.ReopenWork;

public sealed record ReopenWorkCommand(Guid WorkId, Guid ActorId, string Reason) : ICommand<Unit>;
