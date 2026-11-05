using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.ActivateUser;

public sealed record ActivateUserCommand(Guid UserId) : ICommand<Unit>;
