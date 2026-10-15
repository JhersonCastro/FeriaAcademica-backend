namespace FeriaAcademica.Application.Features.Notifications.Queries.ListMyNotifications;

/// <summary>Reads straight from the database into a DTO, without going through the entity.</summary>
public interface INotificationReadModel { Task<IReadOnlyList<NotificationDto>> ListAsync(Guid userId, bool unreadOnly, CancellationToken ct); }
