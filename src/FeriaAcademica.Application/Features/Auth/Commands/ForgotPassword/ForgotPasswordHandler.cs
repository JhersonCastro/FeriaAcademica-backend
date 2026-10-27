using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Domain.Users;
using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.ForgotPassword;

internal sealed class ForgotPasswordHandler(IUserAccounts accounts, IAccountEmails emails) : ICommandHandler<ForgotPasswordCommand, Unit>
{
    public async ValueTask<Unit> Handle(ForgotPasswordCommand c, CancellationToken ct)
    {
        var email = UserInput.Email(c.Email);
        var user = await accounts.FindByEmailAsync(email, ct);
        if (user is null || !UserAccount.CanRecoverPassword(user.Status)) return Unit.Value;

        var token = await accounts.GeneratePasswordTokenAsync(user.Id, ct);
        await emails.SendPasswordResetAsync(user.Email, user.FullName, user.Id, token, ct);
        return Unit.Value;
    }
}
