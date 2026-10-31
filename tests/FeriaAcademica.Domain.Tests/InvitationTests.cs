using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of how a professor's invitation turns a student into a participant.</summary>
public class InvitationTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Professor = Guid.NewGuid();

    private static Invitation NewInvitation() =>
        Invitation.Create(Guid.NewGuid(), "ana@unicauca.edu.co", EventRole.Participant, InvitationOrigin.Invitation, Professor, "token", T0.AddDays(7));

    [Fact]
    public void The_invited_person_can_accept_a_pending_invitation()
    {
        var invitation = NewInvitation();
        var user = Guid.NewGuid();

        invitation.Accept(user, "ANA@unicauca.edu.co", T0);

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Equal(user, invitation.UserId);
    }

    [Fact]
    public void An_invitation_cannot_be_accepted_by_another_email()
    {
        var invitation = NewInvitation();

        Assert.Throws<DomainException>(() => invitation.Accept(Guid.NewGuid(), "luis@unicauca.edu.co", T0));
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }

    [Fact]
    public void An_expired_invitation_cannot_be_accepted()
    {
        var invitation = NewInvitation();

        Assert.Throws<DomainException>(() => invitation.Accept(Guid.NewGuid(), "ana@unicauca.edu.co", T0.AddDays(8)));
    }

    [Fact]
    public void An_invitation_can_only_be_accepted_once()
    {
        var invitation = NewInvitation();
        invitation.Accept(Guid.NewGuid(), "ana@unicauca.edu.co", T0);

        Assert.Throws<DomainException>(() => invitation.Accept(Guid.NewGuid(), "ana@unicauca.edu.co", T0));
    }
}
