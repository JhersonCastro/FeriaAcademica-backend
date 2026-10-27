using System.Security.Cryptography;
using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Invitations.Commands.SendInvitation;

internal sealed class SendInvitationHandler(
    IAcademicEventRepository events, IRoleAssignmentRepository roles, IInvitationRepository invitations, IUnitOfWork uow, IAccountEmails emails)
    : ICommandHandler<SendInvitationCommand, Guid>
{
    public async ValueTask<Guid> Handle(SendInvitationCommand c, CancellationToken ct)
    {
        var email = UserInput.Email(c.Email);
        if (!Enum.TryParse<EventRole>(c.Role, ignoreCase: true, out var role)) throw new DomainException($"Unknown role '{c.Role}'.");
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");

        var senderRoles = await roles.GetRolesAsync(c.SenderId, academicEvent.Id, ct);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var invitation = Invitation.Send(academicEvent.Id, email, role, c.SenderId, senderRoles, token, DateTime.UtcNow);

        await invitations.AddAsync(invitation, ct);
        await uow.SaveChangesAsync(ct);
        await emails.SendInvitationAsync(email, academicEvent.Name, role.ToString(), token, ct);
        return invitation.Id;
    }
}
