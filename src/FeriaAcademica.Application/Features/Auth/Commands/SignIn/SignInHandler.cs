using FeriaAcademica.Application.Features.Users.Queries.GetUser;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Users;
using Mediator;

namespace FeriaAcademica.Application.Features.Auth.Commands.SignIn;

internal sealed class SignInHandler(IAuthService auth, IUserReadModel users) : ICommandHandler<SignInCommand, UserDto>
{
    public async ValueTask<UserDto> Handle(SignInCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.Email) || string.IsNullOrEmpty(c.Password)) throw new DomainException("Email and password are required.");

        var account = await auth.CheckPasswordAsync(c.Email.Trim(), c.Password, ct);
        UserAccount.EnsureCanSignIn(account.Status); // only reached with the right password, so it reveals nothing to a stranger
        await auth.SignInAsync(account.Id, c.Remember, ct);
        return await users.GetAsync(account.Id, ct) ?? throw new DomainException("User not found.");
    }
}
