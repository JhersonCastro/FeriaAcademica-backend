using FeriaAcademica.Domain.Common;
using Mediator;

namespace FeriaAcademica.Application.Features.Users.Commands.UpdateMyProfile;

internal sealed class UpdateMyProfileHandler(IUserAccounts accounts) : ICommandHandler<UpdateMyProfileCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateMyProfileCommand c, CancellationToken ct)
    {
        var profile = UserInput.Profile(c.FullName, c.PhoneNumber, c.BirthDate, c.GuardianName, c.GuardianEmail);
        var user = await accounts.FindAsync(c.UserId, ct) ?? throw new DomainException("User not found.");
        await accounts.UpdateProfileAsync(user.Id, profile, ct);
        return Unit.Value;
    }
}
