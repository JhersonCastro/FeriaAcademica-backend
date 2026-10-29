using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Notifications;
using Mediator;

namespace FeriaAcademica.Application.Features.Notifications.Commands.MarkNotificationAsRead;

internal sealed class MarkNotificationAsReadHandler(INotificationRepository notifications, IUnitOfWork uow)
    : ICommandHandler<MarkNotificationAsReadCommand, Unit>
{
    public async ValueTask<Unit> Handle(MarkNotificationAsReadCommand c, CancellationToken ct)
    {
        var notification = await notifications.GetAsync(c.NotificationId, ct) ?? throw new DomainException("Notification not found.");
        notification.MarkAsRead(c.UserId);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
