using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of who reviews the works of a participant: the professor who invited them.</summary>
public class ParticipationReviewerTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Event = Guid.NewGuid();
    private static readonly Guid Professor = Guid.NewGuid();

    private static Invitation Invited(Guid? sender = null) =>
        Invitation.Create(Event, "ana@feria.test", EventRole.Participant, InvitationOrigin.Invitation, sender ?? Professor, "token", T0.AddDays(7));

    [Fact]
    public void The_reviewer_is_the_professor_who_sent_the_invitation()
    {
        var invitation = Invited();
        var participation = Participation.Create(Event, Guid.NewGuid(), invitation.Id);

        Assert.Equal(Professor, participation.ReviewerFrom(invitation));
    }

    [Fact]
    public void A_participation_without_invitation_has_no_reviewer()
    {
        var participation = Participation.Create(Event, Guid.NewGuid());

        Assert.Throws<DomainException>(() => participation.ReviewerFrom(null));
    }

    [Fact]
    public void An_invitation_the_participation_did_not_come_from_is_not_its_reviewer()
    {
        var participation = Participation.Create(Event, Guid.NewGuid(), Invited().Id);

        Assert.Throws<DomainException>(() => participation.ReviewerFrom(Invited(Guid.NewGuid())));
    }

    [Fact]
    public void An_invitation_without_sender_gives_no_reviewer()
    {
        var invitation = Invitation.Create(Event, "ana@feria.test", EventRole.Participant, InvitationOrigin.Request, null, "token", T0.AddDays(7));
        var participation = Participation.Create(Event, Guid.NewGuid(), invitation.Id);

        Assert.Throws<DomainException>(() => participation.ReviewerFrom(invitation));
    }
}
