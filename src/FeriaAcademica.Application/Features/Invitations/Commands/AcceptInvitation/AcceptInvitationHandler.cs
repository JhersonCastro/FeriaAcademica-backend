using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Users;
using Mediator;

namespace FeriaAcademica.Application.Features.Invitations.Commands.AcceptInvitation;

internal sealed class AcceptInvitationHandler(IInvitationRepository invitations, IUserAccounts accounts, IRoleAssignmentRepository roles, IUnitOfWork uow)
    : ICommandHandler<AcceptInvitationCommand, Unit>
{
    public async ValueTask<Unit> Handle(AcceptInvitationCommand c, CancellationToken ct)
    {
        var invitation = await invitations.GetByTokenAsync(c.Token, ct) ?? throw new DomainException("Invitation not found.");
        var now = DateTime.UtcNow;

        Guid userId;
        var existing = await accounts.FindByEmailAsync(invitation.Email, ct);
        if (existing is not null)
        {
            if (c.CurrentUserId != existing.Id) throw new DomainException("Sign in with the invited account to accept this invitation.");
            userId = existing.Id;
        }
        else
        {
            var profile = UserInput.Profile(c.FullName, null, null, null, null);
            var password = UserInput.Password(c.Password);
            invitation.EnsureCanBeAcceptedBy(invitation.Email, now); // before creating an account for a dead invitation
            userId = await accounts.CreateAsync(invitation.Email, profile, password, UserStatus.Active, ct);
        }

        var assignment = invitation.Accept(userId, invitation.Email, now);
        var current = await roles.GetRolesAsync(userId, invitation.EventId, ct);
        if (!current.Contains(assignment.Role)) await roles.AddAsync(assignment, ct);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
