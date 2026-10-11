using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.SubmitWork;

public sealed record SubmitWorkCommand(Guid WorkId, Guid ActorId) : ICommand<Unit>;
