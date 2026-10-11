using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.SignOut;

internal sealed class SignOutHandler(IAuthService auth) : ICommandHandler<SignOutCommand, bool>
{
    public async ValueTask<bool> Handle(SignOutCommand c, CancellationToken ct)
    {
        await auth.SignOutAsync();
        return true;
    }
}
