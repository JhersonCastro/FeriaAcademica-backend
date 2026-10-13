using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Users;
using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.DeactivateUser;

internal sealed class DeactivateUserHandler(IUserAccounts accounts) : ICommandHandler<DeactivateUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeactivateUserCommand c, CancellationToken ct)
    {
        var user = await accounts.FindAsync(c.UserId, ct) ?? throw new DomainException("User not found.");
        var next = UserAccount.Deactivate(user.Status, user.Id, c.ActorId);
        await accounts.SetStatusAsync(user.Id, next, ct);
        return Unit.Value;
    }
}
