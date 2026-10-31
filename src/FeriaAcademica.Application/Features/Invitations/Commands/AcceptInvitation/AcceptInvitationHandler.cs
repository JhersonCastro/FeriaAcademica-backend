using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Invitations.Commands.AcceptInvitation;

internal sealed class AcceptInvitationHandler(
    IInvitationRepository invitations, IAcademicEventRepository events, IParticipationRepository participations, IUnitOfWork uow)
    : ICommandHandler<AcceptInvitationCommand, Guid>
{
    public async ValueTask<Guid> Handle(AcceptInvitationCommand c, CancellationToken ct)
    {
        var invitation = await invitations.GetByTokenAsync(c.Token, ct) ?? throw new DomainException("Invitation not found.");
        if (invitation.Role != EventRole.Participant) throw new DomainException("This invitation is not for a participant.");

        var academicEvent = await events.GetAsync(invitation.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.EnsureAcceptsParticipants();
        if (await participations.GetByEventAndUserAsync(academicEvent.Id, c.UserId, ct) is not null)
            throw new DomainException("You already participate in this event.");

        invitation.Accept(c.UserId, c.UserEmail, DateTime.UtcNow);
        var participation = Participation.Create(academicEvent.Id, c.UserId, invitation.Id);
        await participations.AddAsync(participation, ct);
        await uow.SaveChangesAsync(ct);
        return participation.Id;
    }
}
