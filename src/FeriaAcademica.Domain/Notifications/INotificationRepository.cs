namespace FeriaAcademica.Domain.Notifications;

/// <summary>Persistence of notifications; implemented in Infrastructure.</summary>
public interface INotificationRepository
{
    Task<Notification?> GetAsync(Guid id, CancellationToken ct);
    Task AddAsync(Notification notification, CancellationToken ct);
}
