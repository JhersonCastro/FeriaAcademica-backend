using System.Security.Claims;
using FeriaAcademica.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using FeriaAcademica.Application.Features.Notifications.Queries.ListMyNotifications;
using FeriaAcademica.Api.Realtime;
using FeriaAcademica.Infrastructure.Realtime;
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

    /// <summary>
    /// Server-Sent Events: stays open and pushes a <c>notification</c> event (a <c>NotificationDto</c>) each time one is created for the
    /// signed-in user. Authenticated by the session cookie. The client loads the current list over REST when it connects.
    /// </summary>
    [HttpGet("stream")]
    public async Task Stream([FromServices] RealtimeHub hub)
    {
        using var subscription = hub.SubscribeToUser(UserId);
        await SseResponse.WriteAsync(HttpContext, subscription, hub.HeartbeatInterval);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        await mediator.Send(new MarkNotificationAsReadCommand(id, UserId), ct);
        return NoContent();
    }
}
