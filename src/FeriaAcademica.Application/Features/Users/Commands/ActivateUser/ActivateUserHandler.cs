using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Users;
using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.ActivateUser;

internal sealed class ActivateUserHandler(IUserAccounts accounts) : ICommandHandler<ActivateUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(ActivateUserCommand c, CancellationToken ct)
    {
        var user = await accounts.FindAsync(c.UserId, ct) ?? throw new DomainException("User not found.");
        var next = UserAccount.Activate(user.Status, user.HasPassword);
        await accounts.SetStatusAsync(user.Id, next, ct);
        return Unit.Value;
    }
}
