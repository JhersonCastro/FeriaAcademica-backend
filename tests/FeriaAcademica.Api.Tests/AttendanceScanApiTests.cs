using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Api.Tests;

/// <summary>T-16: taking attendance by scanning QR codes; nobody can scan their own.</summary>
public class AttendanceScanApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private Guid _eventId;
    private HttpClient _coordinator = default!, _guest = default!;

    public async Task InitializeAsync()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await admin.MyIdAsync(), "Feria 2026", closed: false);

        var coordinatorId = await _api.CreateUserAsync("coordinador@feria.test", "Coordinación");
        await _api.GiveRoleAsync(_eventId, coordinatorId, EventRole.Coordinator);
        await _api.CreateUserAsync("invitado@feria.test", "Invitado");
        _coordinator = await _api.LoginAsync("coordinador@feria.test");
        _guest = await _api.LoginAsync("invitado@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private string Url(string path) => $"/api/events/{_eventId}/{path}";

    private static async Task<string> RegisterAsync(HttpClient client, string url) =>
        (await (await client.PostAsync(url, null)).JsonAsync()).GetProperty("qrToken").GetString()!;

    private static async Task<System.Text.Json.JsonElement> ScanAsync(HttpClient scanner, string url, string qr)
    {
        var response = await scanner.PostAsJsonAsync(url, new { qrToken = qr });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.JsonAsync();
    }

    [Fact]
    public async Task A_coordinator_scanning_a_guests_QR_registers_the_attendance()
    {
        var guestQr = await RegisterAsync(_guest, Url("registrations"));

        var result = await ScanAsync(_coordinator, Url("attendance/scan"), guestQr);

        Assert.Equal("Valid", result.GetProperty("outcome").GetString());
        Assert.Equal("Invitado", result.GetProperty("attendeeName").GetString());
    }

    [Fact]
    public async Task A_coordinator_scanning_their_own_QR_is_refused_with_a_clear_message_and_does_not_get_in()
    {
        var ownQr = await RegisterAsync(_coordinator, Url("registrations"));

        var result = await ScanAsync(_coordinator, Url("attendance/scan"), ownQr);

        Assert.Equal("Invalid", result.GetProperty("outcome").GetString());
        Assert.Equal("You cannot scan your own QR code.", result.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("enteredAt").ValueKind);

        var summary = await (await _coordinator.GetAsync(Url("attendance/summary"))).JsonAsync();
        Assert.Equal(0, summary.GetProperty("entered").GetInt32());
        var registration = await (await _coordinator.GetAsync(Url("registrations/me"))).JsonAsync();
        Assert.Equal(JsonValueKind.Null, registration.GetProperty("enteredAt").ValueKind);
    }

    [Fact]
    public async Task The_super_admin_cannot_scan_their_own_QR_either()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        var ownQr = await RegisterAsync(admin, Url("registrations"));

        var result = await ScanAsync(admin, Url("attendance/scan"), ownQr);

        Assert.Equal("Invalid", result.GetProperty("outcome").GetString());
    }

    [Fact]
    public async Task Refusing_the_own_QR_does_not_block_someone_else_from_scanning_it_later()
    {
        var ownQr = await RegisterAsync(_coordinator, Url("registrations"));
        await ScanAsync(_coordinator, Url("attendance/scan"), ownQr);

        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        var result = await ScanAsync(admin, Url("attendance/scan"), ownQr);

        Assert.Equal("Valid", result.GetProperty("outcome").GetString());
    }
}
