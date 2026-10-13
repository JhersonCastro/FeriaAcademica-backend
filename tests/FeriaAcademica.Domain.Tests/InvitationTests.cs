using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Tests;

public class InvitationTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Event = Guid.NewGuid();
    private static readonly Guid Teacher = Guid.NewGuid();
    private static readonly EventRole[] TeacherRoles = [EventRole.Teacher];

    private static Invitation Sent(EventRole role = EventRole.Participant, string email = "ana@example.org") =>
        Invitation.Send(Event, email, role, Teacher, TeacherRoles, "token", T0);

    // ---- Send ----

    [Theory]
    [InlineData(EventRole.Participant)]
    [InlineData(EventRole.Attendee)]
    public void A_teacher_can_invite_students_and_guests(EventRole role)
    {
        var invitation = Sent(role);

        Assert.Equal(role, invitation.Role);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Equal(InvitationOrigin.Invitation, invitation.Origin);
        Assert.Equal(Teacher, invitation.SenderId);
        Assert.Equal(T0 + Invitation.Lifetime, invitation.ExpiresAt);
    }

    [Fact]
    public void Only_a_teacher_of_the_event_can_invite()
    {
        Assert.Throws<DomainException>(() => Invitation.Send(Event, "ana@example.org", EventRole.Participant, Teacher, [EventRole.Jury], "token", T0));
        Assert.Throws<DomainException>(() => Invitation.Send(Event, "ana@example.org", EventRole.Participant, Teacher, [], "token", T0));
    }

    [Theory]
    [InlineData(EventRole.Coordinator)]
    [InlineData(EventRole.Teacher)]
    [InlineData(EventRole.Jury)]
    public void A_teacher_cannot_invite_other_roles(EventRole role) =>
        Assert.Throws<DomainException>(() => Sent(role));

    // ---- Accept ----

    [Fact]
    public void Accepting_gives_the_user_the_invited_role_in_the_event()
    {
        var invitation = Sent(EventRole.Attendee);
        var user = Guid.NewGuid();

        var assignment = invitation.Accept(user, "ana@example.org", T0.AddDays(1));

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Equal(user, invitation.UserId);
        Assert.Equal((user, Event, EventRole.Attendee), (assignment.UserId, assignment.EventId, assignment.Role));
    }

    [Fact]
    public void The_email_comparison_ignores_case_and_spaces() =>
        Sent().Accept(Guid.NewGuid(), "  ANA@Example.org ", T0);

    [Fact]
    public void Only_the_invited_email_can_accept()
    {
        var invitation = Sent();
        Assert.Throws<DomainException>(() => invitation.Accept(Guid.NewGuid(), "someone@example.org", T0));
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }

    [Fact]
    public void An_expired_invitation_cannot_be_accepted()
    {
        var invitation = Sent();
        var ex = Assert.Throws<DomainException>(() => invitation.Accept(Guid.NewGuid(), "ana@example.org", T0 + Invitation.Lifetime));
        Assert.Contains("expired", ex.Message);
    }

    [Fact]
    public void An_invitation_can_only_be_accepted_once()
    {
        var invitation = Sent();
        invitation.Accept(Guid.NewGuid(), "ana@example.org", T0);
        Assert.Throws<DomainException>(() => invitation.Accept(Guid.NewGuid(), "ana@example.org", T0));
    }
}
