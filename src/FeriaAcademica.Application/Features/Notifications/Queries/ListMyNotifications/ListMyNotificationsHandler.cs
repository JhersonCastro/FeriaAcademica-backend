using Mediator;

namespace FeriaAcademica.Application.Features.Notifications.Queries.ListMyNotifications;

internal sealed class ListMyNotificationsHandler(INotificationReadModel readModel) : IQueryHandler<ListMyNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async ValueTask<IReadOnlyList<NotificationDto>> Handle(ListMyNotificationsQuery q, CancellationToken ct) =>
        await readModel.ListAsync(q.UserId, q.UnreadOnly, ct);
}
