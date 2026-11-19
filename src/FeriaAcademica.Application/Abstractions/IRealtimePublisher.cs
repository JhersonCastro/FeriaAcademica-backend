namespace FeriaAcademica.Application.Abstractions;

/// <summary>Names of the real-time events the API pushes to connected clients (Server-Sent Events).</summary>
public static class RealtimeEvents
{
    /// <summary>A notification was created for the user; the data is a <c>NotificationDto</c>.</summary>
    public const string Notification = "notification";
    /// <summary>The turnout of an event changed; the data is an <c>AttendanceSummaryDto</c>.</summary>
    public const string Summary = "summary";
    /// <summary>A person just entered an event; the data is an <c>EventRegistrationDto</c>.</summary>
    public const string Entered = "entered";
}

/// <summary>
/// Pushes a message to whoever is connected right now. Handlers call it only after the change was saved, so nothing is announced
/// that did not reach the database. Nobody connected means nothing happens: clients load the current state over REST when they connect.
/// </summary>
public interface IRealtimePublisher
{
    void ToUser(Guid userId, string eventName, object data);
    void ToEvent(Guid eventId, string eventName, object data);
}
