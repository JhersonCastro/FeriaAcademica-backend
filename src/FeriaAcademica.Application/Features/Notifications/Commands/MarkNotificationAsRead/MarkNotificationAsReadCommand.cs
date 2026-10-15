using Mediator;

namespace FeriaAcademica.Application.Features.Notifications.Commands.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId, Guid UserId) : ICommand<Unit>;
