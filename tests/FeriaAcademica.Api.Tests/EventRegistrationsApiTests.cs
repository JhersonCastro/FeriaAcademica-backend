using System.Net;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Api.Tests;

/// <summary>T-13: the list of people registered for a fair.</summary>
public class EventRegistrationsApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private HttpClient _admin = default!;
    private Guid _eventId;

    public async Task InitializeAsync()
    {
        _admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await _admin.MyIdAsync(), "Feria 2026", closed: false);
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private string Url => $"/api/events/{_eventId}/registrations";

    [Fact]
    public async Task It_lists_everyone_registered_by_name_with_whether_they_entered()
    {
        await _api.RegisterAsync(_eventId, await _api.CreateUserAsync("zoe@feria.test", "Zoe Muñoz"), attended: false);
        await _api.RegisterAsync(_eventId, await _api.CreateUserAsync("ana@feria.test", "Ana Gómez"), attended: true);

        var list = (await (await _admin.GetAsync(Url)).JsonAsync()).EnumerateArray().ToArray();

        Assert.Equal(["Ana Gómez", "Zoe Muñoz"], list.Select(r => r.GetProperty("fullName").GetString()));
        Assert.Equal("ana@feria.test", list[0].GetProperty("email").GetString());
        Assert.Equal("Registered", list[0].GetProperty("status").GetString());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, list[0].GetProperty("enteredAt").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, list[1].GetProperty("enteredAt").ValueKind);
    }

    [Fact]
    public async Task Registrations_of_other_fairs_are_not_listed()
    {
        var other = await _api.CreateEventAsync(await _admin.MyIdAsync(), "Otra feria", closed: false);
        await _api.RegisterAsync(other, await _api.CreateUserAsync("ana@feria.test", "Ana Gómez"), attended: false);

        Assert.Empty((await (await _admin.GetAsync(Url)).JsonAsync()).EnumerateArray());
    }

    [Theory]
    [InlineData(EventRole.Coordinator)]
    [InlineData(EventRole.Teacher)]
    public async Task The_coordinators_and_teachers_of_the_fair_can_see_it(EventRole role)
    {
        var staff = await _api.CreateUserAsync("staff@feria.test");
        await _api.GiveRoleAsync(_eventId, staff, role);

        Assert.Equal(HttpStatusCode.OK, (await (await _api.LoginAsync("staff@feria.test")).GetAsync(Url)).StatusCode);
    }

    [Fact]
    public async Task A_participant_cannot_see_it_and_it_needs_a_session()
    {
        var student = await _api.CreateUserAsync("estudiante@feria.test");
        await _api.GiveRoleAsync(_eventId, student, EventRole.Participant);

        var response = await (await _api.LoginAsync("estudiante@feria.test")).GetAsync(Url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("registrations", await response.ProblemDetailAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.NewClient().GetAsync(Url)).StatusCode);
    }

    [Fact]
    public async Task An_unknown_fair_is_refused()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _admin.GetAsync($"/api/events/{Guid.NewGuid()}/registrations")).StatusCode);
    }
}
