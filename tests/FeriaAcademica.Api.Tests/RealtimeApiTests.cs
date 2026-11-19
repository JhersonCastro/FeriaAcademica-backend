using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Infrastructure.Realtime;
using Microsoft.Extensions.DependencyInjection;

namespace FeriaAcademica.Api.Tests;

/// <summary>Server-Sent Events: notifications for their user and turnout for the staff of an event, pushed after the change is saved.</summary>
public class RealtimeApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private Guid _eventId, _otherEventId, _professorId, _participantId, _guestId, _coordinatorId;
    private HttpClient _coordinator = default!, _guest = default!, _professor = default!;

    public async Task InitializeAsync()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        var adminId = await admin.MyIdAsync();
        _eventId = await _api.CreateEventAsync(adminId, "Feria 2026", closed: false);
        _otherEventId = await _api.CreateEventAsync(adminId, "Otra feria", closed: false);

        _coordinatorId = await _api.CreateUserAsync("coordinador@feria.test", "Coordinación");
        await _api.GiveRoleAsync(_eventId, _coordinatorId, EventRole.Coordinator);
        await _api.GiveRoleAsync(_otherEventId, _coordinatorId, EventRole.Coordinator);
        _professorId = await _api.CreateUserAsync("profesor@feria.test", "Profesor");
        _participantId = await _api.CreateUserAsync("estudiante@feria.test", "Estudiante");
        _guestId = await _api.CreateUserAsync("invitado@feria.test", "Invitado");
        await _api.CreateUserAsync("otra@feria.test", "Otra persona");

        _coordinator = await _api.LoginAsync("coordinador@feria.test");
        _guest = await _api.LoginAsync("invitado@feria.test");
        _professor = await _api.LoginAsync("profesor@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private RealtimeHub Hub => _api.Services.GetRequiredService<RealtimeHub>();
    private string EventUrl(string path, Guid? eventId = null) => $"/api/events/{eventId ?? _eventId}/{path}";

    /// <summary>Each stream gets its own client (and cookie), like a separate browser tab.</summary>
    private async Task<SseReader> OpenAsync(string email, string url) => await SseReader.OpenAsync(await _api.LoginAsync(email), url);

    private static async Task<string> RegisterAsync(HttpClient client, string url) =>
        (await (await client.PostAsync(url, null)).JsonAsync()).GetProperty("qrToken").GetString()!;

    private async Task<string> ScanAsync(string qr, Guid? eventId = null) =>
        (await (await _coordinator.PostAsJsonAsync(EventUrl("attendance/scan", eventId), new { qrToken = qr })).JsonAsync()).GetProperty("outcome").GetString()!;

    private async Task DecideAsync(Guid workId) =>
        Assert.Equal(HttpStatusCode.NoContent, (await _professor.PostAsJsonAsync($"/api/reviews/{workId}/decision", new { decision = "Approved" })).StatusCode);

    // ---- transport ----

    [Fact]
    public async Task The_stream_is_an_event_stream_that_proxies_must_not_buffer()
    {
        await using var stream = await OpenAsync("estudiante@feria.test", "/api/notifications/stream");

        Assert.Equal("text/event-stream", stream.Response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no-cache", stream.Response.Headers.CacheControl?.ToString());
        Assert.Equal("no", stream.Response.Headers.GetValues("X-Accel-Buffering").Single());
    }

    [Fact]
    public async Task An_idle_connection_receives_a_heartbeat()
    {
        Hub.HeartbeatInterval = TimeSpan.FromMilliseconds(150);
        await using var stream = await OpenAsync("estudiante@feria.test", "/api/notifications/stream");

        Assert.Equal(": connected", await stream.NextCommentAsync());
        Assert.Equal(": ping", await stream.NextCommentAsync());
    }

    // ---- notifications ----

    [Fact]
    public async Task A_notification_reaches_the_stream_of_its_user_and_not_the_one_of_anybody_else()
    {
        var workId = await _api.CreateReviewedWorkAsync(_eventId, _participantId, _professorId);
        await using var participant = await OpenAsync("estudiante@feria.test", "/api/notifications/stream");
        await using var other = await OpenAsync("otra@feria.test", "/api/notifications/stream");

        await DecideAsync(workId);

        var (name, data) = await participant.NextAsync();
        Assert.Equal("notification", name);
        Assert.Equal("WorkReviewed", data.GetProperty("type").GetString());
        Assert.Equal(_eventId, data.GetProperty("eventId").GetGuid());
        Assert.False(data.GetProperty("isRead").GetBoolean());
        Assert.EndsWith("was approved.", data.GetProperty("message").GetString());
        await other.ExpectSilenceAsync();

        // The pushed notification is the one that REST lists.
        var listed = await (await (await _api.LoginAsync("estudiante@feria.test")).GetAsync("/api/notifications")).JsonAsync();
        Assert.Equal(listed[0].GetProperty("id").GetGuid(), data.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Several_connections_of_the_same_user_all_receive_the_notification()
    {
        var workId = await _api.CreateReviewedWorkAsync(_eventId, _participantId, _professorId);
        await using var first = await OpenAsync("estudiante@feria.test", "/api/notifications/stream");
        await using var second = await OpenAsync("estudiante@feria.test", "/api/notifications/stream");

        await DecideAsync(workId);

        Assert.Equal("notification", (await first.NextAsync()).Event);
        Assert.Equal("notification", (await second.NextAsync()).Event);
    }

    [Fact]
    public async Task Without_a_session_the_streams_answer_401()
    {
        var anonymous = _api.NewClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/notifications/stream")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(EventUrl("attendance/stream"))).StatusCode);
    }

    // ---- attendance ----

    [Fact]
    public async Task A_valid_check_in_sends_entered_and_then_the_summary_to_the_staff_of_that_event_only()
    {
        var qr = await RegisterAsync(_guest, EventUrl("registrations"));
        await using var staff = await OpenAsync("coordinador@feria.test", EventUrl("attendance/stream"));
        await using var otherEvent = await OpenAsync("coordinador@feria.test", EventUrl("attendance/stream", _otherEventId));

        Assert.Equal("Valid", await ScanAsync(qr));

        var (entered, person) = await staff.NextAsync();
        Assert.Equal("entered", entered);
        Assert.Equal("Invitado", person.GetProperty("fullName").GetString());
        Assert.Equal("invitado@feria.test", person.GetProperty("email").GetString());
        Assert.Equal("Registered", person.GetProperty("status").GetString());
        Assert.Equal(_guestId, person.GetProperty("userId").GetGuid());
        Assert.NotEqual(JsonValueKind.Null, person.GetProperty("enteredAt").ValueKind);

        var (summary, counts) = await staff.NextAsync();
        Assert.Equal("summary", summary);
        Assert.Equal(_eventId, counts.GetProperty("eventId").GetGuid());
        Assert.Equal((1, 1, 0), (counts.GetProperty("registered").GetInt32(), counts.GetProperty("entered").GetInt32(), counts.GetProperty("pending").GetInt32()));
        Assert.NotEqual(JsonValueKind.Null, counts.GetProperty("lastEntryAt").ValueKind);

        await otherEvent.ExpectSilenceAsync();
    }

    [Fact]
    public async Task Registering_sends_only_the_summary()
    {
        await using var staff = await OpenAsync("coordinador@feria.test", EventUrl("attendance/stream"));

        await RegisterAsync(_guest, EventUrl("registrations"));

        var (name, counts) = await staff.NextAsync();
        Assert.Equal("summary", name);
        Assert.Equal((1, 0, 1), (counts.GetProperty("registered").GetInt32(), counts.GetProperty("entered").GetInt32(), counts.GetProperty("pending").GetInt32()));
        await staff.ExpectSilenceAsync();

        // Asking for the same registration again changes nothing, so it announces nothing.
        await RegisterAsync(_guest, EventUrl("registrations"));
        await staff.ExpectSilenceAsync();
    }

    [Fact]
    public async Task A_repeated_scan_an_invalid_code_and_scanning_your_own_QR_do_not_send_entered()
    {
        var qr = await RegisterAsync(_guest, EventUrl("registrations"));
        var ownQr = await RegisterAsync(_coordinator, EventUrl("registrations"));
        await using var staff = await OpenAsync("coordinador@feria.test", EventUrl("attendance/stream"));

        Assert.Equal("Valid", await ScanAsync(qr));
        Assert.Equal("entered", (await staff.NextAsync()).Event);
        Assert.Equal("summary", (await staff.NextAsync()).Event);

        Assert.Equal("AlreadyEntered", await ScanAsync(qr));
        Assert.Equal("Invalid", await ScanAsync("not-a-qr"));
        Assert.Equal("Invalid", await ScanAsync(ownQr));
        await staff.ExpectSilenceAsync();
    }

    [Fact]
    public async Task Staff_connected_at_the_same_time_all_get_the_event()
    {
        var qr = await RegisterAsync(_guest, EventUrl("registrations"));
        await using var first = await OpenAsync("coordinador@feria.test", EventUrl("attendance/stream"));
        await using var second = await OpenAsync(ApiFactory.AdminEmail, EventUrl("attendance/stream"));

        await ScanAsync(qr);

        Assert.Equal("entered", (await first.NextAsync()).Event);
        Assert.Equal("entered", (await second.NextAsync()).Event);
    }

    [Fact]
    public async Task Someone_without_access_to_the_turnout_is_refused_like_in_the_summary()
    {
        var summary = await _guest.GetAsync(EventUrl("attendance/summary"));
        var stream = await _guest.GetAsync(EventUrl("attendance/stream"));

        Assert.False(summary.IsSuccessStatusCode);
        Assert.Equal(summary.StatusCode, stream.StatusCode);
        Assert.Equal(await summary.ProblemDetailAsync(), await stream.ProblemDetailAsync());
        Assert.Equal(0, Hub.SubscriberCountForEvent(_eventId));   // it was refused before subscribing
    }

    // ---- no leaks ----

    [Fact]
    public async Task Closing_the_connection_removes_the_subscriber()
    {
        var notifications = await OpenAsync("estudiante@feria.test", "/api/notifications/stream");
        var attendance = await OpenAsync("coordinador@feria.test", EventUrl("attendance/stream"));
        Assert.Equal(1, Hub.SubscriberCountForUser(_participantId));
        Assert.Equal(1, Hub.SubscriberCountForEvent(_eventId));

        await notifications.DisposeAsync();
        await attendance.DisposeAsync();

        await WaitUntilAsync(() => Hub.SubscriberCountForUser(_participantId) == 0 && Hub.SubscriberCountForEvent(_eventId) == 0);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(50);
        Assert.True(condition(), "The subscriber was not removed after the client disconnected.");
    }
}
