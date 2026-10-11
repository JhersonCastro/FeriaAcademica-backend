using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.RegisterUser;

internal sealed class RegisterUserHandler(IAuthService auth) : ICommandHandler<RegisterUserCommand, UserDto>
{
    public async ValueTask<UserDto> Handle(RegisterUserCommand c, CancellationToken ct) =>
        await auth.RegisterAsync(c.Email, c.Password, c.FullName, ct);
}
