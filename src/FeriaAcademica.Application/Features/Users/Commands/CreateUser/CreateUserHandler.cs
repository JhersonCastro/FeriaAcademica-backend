using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Users;
using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.CreateUser;

internal sealed class CreateUserHandler(IUserAccounts accounts, IAccountEmails emails) : ICommandHandler<CreateUserCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateUserCommand c, CancellationToken ct)
    {
        var email = UserInput.Email(c.Email);
        var profile = UserInput.Profile(c.FullName, c.PhoneNumber, null, null, null);

        var id = await accounts.CreateAsync(email, profile, password: null, UserStatus.Pending, ct);
        var token = await accounts.GeneratePasswordTokenAsync(id, ct);
        await emails.SendSetPasswordAsync(email, profile.FullName, id, token, ct);
        return id;
    }
}
