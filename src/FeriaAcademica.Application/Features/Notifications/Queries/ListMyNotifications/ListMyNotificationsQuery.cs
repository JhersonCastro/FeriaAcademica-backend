using Mediator;

namespace FeriaAcademica.Application.Features.Notifications.Queries.ListMyNotifications;

public sealed record ListMyNotificationsQuery(Guid UserId, bool UnreadOnly) : IQuery<IReadOnlyList<NotificationDto>>;
