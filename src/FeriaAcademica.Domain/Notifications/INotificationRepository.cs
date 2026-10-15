namespace FeriaAcademica.Domain.Notifications;

public interface INotificationRepository
{
    Task<Notification?> GetAsync(Guid id, CancellationToken ct);
    Task AddAsync(Notification notification, CancellationToken ct);
}
