using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.RejectWork;

public sealed record RejectWorkCommand(Guid WorkId, Guid ReviewerId, string Observations) : ICommand<Unit>;
