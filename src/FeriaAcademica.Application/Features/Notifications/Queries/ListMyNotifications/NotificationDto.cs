using FeriaAcademica.Domain.Notifications;

namespace FeriaAcademica.Application.Features.Notifications.Queries.ListMyNotifications;

public sealed record NotificationDto(Guid Id, Guid? EventId, string Type, string Message, bool IsRead, DateTime CreatedAt)
{
    public static NotificationDto From(Notification n) => new(n.Id, n.EventId, n.Type, n.Message, n.IsRead, n.CreatedAt);
}
