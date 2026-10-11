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
}

public sealed class Invitation : Entity
{
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
}

public sealed class Participation : Entity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? InvitationId { get; private set; }

    private Participation() { } // EF Core

    public static Participation Create(Guid eventId, Guid userId, Guid? invitationId = null) =>
        new() { EventId = eventId, UserId = userId, InvitationId = invitationId };
}
