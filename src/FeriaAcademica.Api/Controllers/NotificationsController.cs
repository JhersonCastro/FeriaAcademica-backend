using System.Security.Claims;
using FeriaAcademica.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using FeriaAcademica.Application.Features.Notifications.Queries.ListMyNotifications;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/notifications"), Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IReadOnlyList<NotificationDto>> Mine([FromQuery] bool unreadOnly, CancellationToken ct) =>
        await mediator.Send(new ListMyNotificationsQuery(UserId, unreadOnly), ct);

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        await mediator.Send(new MarkNotificationAsReadCommand(id, UserId), ct);
        return NoContent();
    }
}
