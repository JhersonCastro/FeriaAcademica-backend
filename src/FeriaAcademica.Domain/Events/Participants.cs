using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Events;

public enum EventRole { Coordinator, Teacher, Jury, Participant, Attendee }
public enum InvitationOrigin { Invitation, Request }
public enum InvitationStatus { Pending, Accepted, Declined, Expired, Cancelled }

public sealed class RoleAssignment : Entity
{
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public EventRole Role { get; private set; }

    private RoleAssignment() { } // EF Core

    public static RoleAssignment Create(Guid userId, Guid eventId, EventRole role) =>
        new() { UserId = userId, EventId = eventId, Role = role };

    /// <summary>Roles belong to an event, not to the user: the same person can hold several roles, but each one only once per event.</summary>
    public static RoleAssignment Assign(Guid userId, Guid eventId, EventRole role, IReadOnlyCollection<EventRole> currentRoles)
    {
        if (currentRoles.Contains(role)) throw new DomainException($"The user already has the {role} role in this event.");
        return Create(userId, eventId, role);
    }
}

public sealed class Invitation : Entity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
    private static readonly EventRole[] RolesATeacherCanInvite = [EventRole.Participant, EventRole.Attendee];

    public Guid EventId { get; private set; }
    public string Email { get; private set; } = default!;
    public EventRole Role { get; private set; }
    public InvitationOrigin Origin { get; private set; }
    public Guid? SenderId { get; private set; }   // who invites (origin = invitation)
    public Guid? UserId { get; private set; }     // filled in once the person has an account
    public string Token { get; private set; } = default!;
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt { get; private set; }

    private Invitation() { } // EF Core

    public static Invitation Create(Guid eventId, string email, EventRole role, InvitationOrigin origin, Guid? senderId, string token, DateTime expiresAt)
    {
        if (role is EventRole.Coordinator) throw new DomainException("Coordinators cannot be invited.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(token)) throw new DomainException("Token is required.");
        return new Invitation { EventId = eventId, Email = email.Trim(), Role = role, Origin = origin, SenderId = senderId, Token = token, ExpiresAt = expiresAt };
    }

    /// <summary>A teacher of the event invites a student (participant) or a guest (attendee), who may or may not have an account yet.</summary>
    public static Invitation Send(Guid eventId, string email, EventRole role, Guid senderId, IReadOnlyCollection<EventRole> senderRoles, string token, DateTime now)
    {
        if (!senderRoles.Contains(EventRole.Teacher)) throw new DomainException("Only a teacher of the event can send invitations.");
        if (!RolesATeacherCanInvite.Contains(role)) throw new DomainException("A teacher can only invite students (participants) or guests (attendees).");
        return Create(eventId, email, role, InvitationOrigin.Invitation, senderId, token, now + Lifetime);
    }

    public void EnsureCanBeAcceptedBy(string email, DateTime now)
    {
        if (Status != InvitationStatus.Pending) throw new DomainException($"This invitation is no longer pending ({Status}).");
        if (now >= ExpiresAt) throw new DomainException("This invitation has expired.");
        if (!string.Equals(Email, email.Trim(), StringComparison.OrdinalIgnoreCase)) throw new DomainException("This invitation was sent to a different email.");
    }

    /// <summary>Accepting gives the invited user the invitation's role in the event.</summary>
    public RoleAssignment Accept(Guid userId, string email, DateTime now)
    {
        EnsureCanBeAcceptedBy(email, now);
        UserId = userId;
        Status = InvitationStatus.Accepted;
        return RoleAssignment.Create(userId, EventId, Role);
    }
}

public sealed class Participation : Entity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? InvitationId { get; private set; }

    private Participation() { } // EF Core

    public static Participation Create(Guid eventId, Guid userId, Guid? invitationId = null) =>
        new() { EventId = eventId, UserId = userId, InvitationId = invitationId };

    /// <summary>Only the participant can create, edit, submit or withdraw the works of their participation.</summary>
    public void EnsureOwnedBy(Guid userId)
    {
        if (userId != UserId) throw new DomainException("Only the author can manage this work.");
    }
}
