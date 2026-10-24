using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Users;
using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.ResetPassword;

internal sealed class ResetPasswordHandler(IUserAccounts accounts) : ICommandHandler<ResetPasswordCommand, Unit>
{
    public async ValueTask<Unit> Handle(ResetPasswordCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.Token)) throw new DomainException("The link is invalid or has expired.");
        var password = UserInput.Password(c.NewPassword);

        var user = await accounts.FindAsync(c.UserId, ct) ?? throw new DomainException("The link is invalid or has expired.");
        var next = UserAccount.AfterPasswordSet(user.Status);

        await accounts.ResetPasswordAsync(user.Id, c.Token, password, ct);
        if (next != user.Status) await accounts.SetStatusAsync(user.Id, next, ct);
        return Unit.Value;
    }
}
