using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId, Guid ActorId) : ICommand<Unit>;
