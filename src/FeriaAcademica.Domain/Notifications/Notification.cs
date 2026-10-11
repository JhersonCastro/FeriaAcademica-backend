using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Notifications;

public sealed class Notification : Entity
{
    public Guid UserId { get; private set; }
    public Guid? EventId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { } // EF Core

    public static Notification Create(Guid userId, Guid? eventId, string type, string message, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(type)) throw new DomainException("Type is required.");
        if (string.IsNullOrWhiteSpace(message)) throw new DomainException("Message is required.");
        return new Notification { UserId = userId, EventId = eventId, Type = type, Message = message, CreatedAt = now };
    }

    public void MarkAsRead() => IsRead = true;
}
