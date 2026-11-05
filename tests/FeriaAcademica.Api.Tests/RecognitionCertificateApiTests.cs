using System.Net;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-060: those who helped run an event get a recognition certificate as a PDF once the event is over.</summary>
public class RecognitionCertificateApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private Guid _adminId, _personId;
    private HttpClient _person = default!;

    public async Task InitializeAsync()
    {
        _personId = await _api.CreateUserAsync("jurado@feria.test", "Laura Mejía");
        _adminId = await (await _api.LoginAsync(ApiFactory.AdminEmail)).MyIdAsync();
        _person = await _api.LoginAsync("jurado@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static string Pdf(Guid eventId) => $"/api/certificates/events/{eventId}/Recognition/pdf";

    [Theory]
    [InlineData(EventRole.Coordinator)]
    [InlineData(EventRole.Teacher)]
    [InlineData(EventRole.Jury)]
    public async Task A_person_who_helped_run_a_finished_event_downloads_it_as_PDF(EventRole role)
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        await _api.GiveRoleAsync(eventId, _personId, role);

        var response = await _person.GetAsync(Pdf(eventId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await response.IsPdfAsync());
    }

    [Fact]
    public async Task A_person_with_several_roles_gets_a_single_certificate()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        await _api.GiveRoleAsync(eventId, _personId, EventRole.Jury);
        await _api.GiveRoleAsync(eventId, _personId, EventRole.Teacher);

        Assert.True(await (await _person.GetAsync(Pdf(eventId))).IsPdfAsync());

        var list = await (await _person.GetAsync("/api/certificates/mine")).JsonAsync();
        var certificate = Assert.Single(list.EnumerateArray());
        Assert.Equal("Recognition", certificate.GetProperty("type").GetString());
        Assert.StartsWith("FA-", certificate.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_participant_cannot_download_it()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        await _api.GiveRoleAsync(eventId, _personId, EventRole.Participant);

        var response = await _person.GetAsync(Pdf(eventId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("recognition", await response.ProblemDetailAsync());
        Assert.Empty((await (await _person.GetAsync("/api/certificates/mine")).JsonAsync()).EnumerateArray());
    }

    [Fact]
    public async Task A_role_in_another_event_does_not_count()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        var other = await _api.CreateEventAsync(_adminId, "Otra feria", closed: true);
        await _api.GiveRoleAsync(other, _personId, EventRole.Jury);

        Assert.Equal(HttpStatusCode.BadRequest, (await _person.GetAsync(Pdf(eventId))).StatusCode);
    }

    [Fact]
    public async Task It_is_listed_but_not_available_until_the_event_has_finished()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: false);
        await _api.GiveRoleAsync(eventId, _personId, EventRole.Jury);

        Assert.Equal(HttpStatusCode.BadRequest, (await _person.GetAsync(Pdf(eventId))).StatusCode);

        var certificate = Assert.Single((await (await _person.GetAsync("/api/certificates/mine")).JsonAsync()).EnumerateArray());
        Assert.False(certificate.GetProperty("available").GetBoolean());
    }
}
