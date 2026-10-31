using System.Security.Cryptography;
using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.InviteParticipant;

internal sealed class InviteParticipantHandler(
    IAcademicEventRepository events, IRoleAssignmentRepository roles, IInvitationRepository invitations, IUnitOfWork uow)
    : ICommandHandler<InviteParticipantCommand, InvitationCreated>
{
    private static readonly TimeSpan Validity = TimeSpan.FromDays(7);

    public async ValueTask<InvitationCreated> Handle(InviteParticipantCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.Email)) throw new DomainException("Email is required.");

        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.EnsureAcceptsParticipants();
        if (!await roles.HasRoleAsync(c.ProfessorId, academicEvent.Id, EventRole.Teacher, ct))
            throw new DomainException("Only a professor of this event can invite participants.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var invitation = Invitation.Create(academicEvent.Id, c.Email, EventRole.Participant, InvitationOrigin.Invitation, c.ProfessorId, token,
            DateTime.UtcNow.Add(Validity));
        await invitations.AddAsync(invitation, ct);
        await uow.SaveChangesAsync(ct);
        return new InvitationCreated(invitation.Id, token, invitation.ExpiresAt);
    }
}
