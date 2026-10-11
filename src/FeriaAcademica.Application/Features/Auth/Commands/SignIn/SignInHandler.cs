using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.SignIn;

internal sealed class SignInHandler(IAuthService auth) : ICommandHandler<SignInCommand, UserDto>
{
    public async ValueTask<UserDto> Handle(SignInCommand c, CancellationToken ct) =>
        await auth.SignInAsync(c.Email, c.Password, c.Remember, ct);
}
