using FeriaAcademica.Domain.Common;
using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.UpdateUser;

internal sealed class UpdateUserHandler(IUserAccounts accounts) : ICommandHandler<UpdateUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateUserCommand c, CancellationToken ct)
    {
        var email = UserInput.Email(c.Email);
        var profile = UserInput.Profile(c.FullName, c.PhoneNumber, c.BirthDate, c.GuardianName, c.GuardianEmail);
        var user = await accounts.FindAsync(c.UserId, ct) ?? throw new DomainException("User not found.");

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)) await accounts.ChangeEmailAsync(user.Id, email, ct);
        await accounts.UpdateProfileAsync(user.Id, profile, ct);
        return Unit.Value;
    }
}
