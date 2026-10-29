namespace FeriaAcademica.Application.Features.Notifications.Queries.ListMyNotifications;

public sealed record NotificationDto(Guid Id, Guid? EventId, string Type, string Message, bool IsRead, DateTime CreatedAt);
