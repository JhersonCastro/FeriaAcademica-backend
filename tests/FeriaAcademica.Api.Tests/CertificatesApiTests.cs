using System.Net;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-061: the person downloads their certificate once the event is over, if they attended or participated.</summary>
public class CertificatesApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private Guid _adminId, _personId, _professorId;
    private HttpClient _admin = default!, _person = default!;

    public async Task InitializeAsync()
    {
        _personId = await _api.CreateUserAsync("persona@feria.test", "Ana Gómez");
        _professorId = await _api.CreateUserAsync("profesor@feria.test", "Profesor");
        _admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _adminId = await _admin.MyIdAsync();
        _person = await _api.LoginAsync("persona@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static string AttendancePdf(Guid eventId) => $"/api/certificates/events/{eventId}/Attendance/pdf";
    private static string ParticipationPdf(Guid eventId) => $"/api/certificates/events/{eventId}/Participation/pdf";

    // ---- attendance ----

    [Fact]
    public async Task A_person_who_attended_a_finished_event_downloads_the_attendance_certificate_as_PDF()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        await _api.RegisterAsync(eventId, _personId, attended: true);

        var response = await _person.GetAsync(AttendancePdf(eventId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await response.IsPdfAsync());
        Assert.StartsWith("certificado-feria-2026-FA-", response.Content.Headers.ContentDisposition?.FileName);
    }

    [Fact]
    public async Task Downloading_again_gives_the_same_certificate_code()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        await _api.RegisterAsync(eventId, _personId, attended: true);

        var first = await _person.GetAsync(AttendancePdf(eventId));
        var second = await _person.GetAsync(AttendancePdf(eventId));

        Assert.Equal(first.Content.Headers.ContentDisposition?.FileName, second.Content.Headers.ContentDisposition?.FileName);
        var mine = await (await _person.GetAsync("/api/certificates/mine")).JsonAsync();
        Assert.Single(mine.EnumerateArray());
    }

    [Fact]
    public async Task A_person_who_did_not_attend_cannot_download_the_attendance_certificate()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        await _api.RegisterAsync(eventId, _personId, attended: false);

        var response = await _person.GetAsync(AttendancePdf(eventId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("attended", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task A_person_who_never_registered_cannot_download_it_either()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);

        Assert.Equal(HttpStatusCode.BadRequest, (await _person.GetAsync(AttendancePdf(eventId))).StatusCode);
    }

    [Fact]
    public async Task Nobody_can_download_a_certificate_before_the_event_has_finished()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: false);
        await _api.RegisterAsync(eventId, _personId, attended: true);

        var response = await _person.GetAsync(AttendancePdf(eventId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("finished", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Closing_the_event_makes_the_certificate_available()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: false);
        await _api.RegisterAsync(eventId, _personId, attended: true);
        Assert.Equal(HttpStatusCode.BadRequest, (await _person.GetAsync(AttendancePdf(eventId))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await _admin.PostAsync($"/api/events/{eventId}/close", null)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await _person.GetAsync(AttendancePdf(eventId))).StatusCode);
    }

    [Fact]
    public async Task Only_the_administrator_can_close_an_event_and_only_once()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: false);

        Assert.Equal(HttpStatusCode.Forbidden, (await _person.PostAsync($"/api/events/{eventId}/close", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _admin.PostAsync($"/api/events/{eventId}/close", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _admin.PostAsync($"/api/events/{eventId}/close", null)).StatusCode);
    }

    // ---- participation ----

    [Fact]
    public async Task A_participant_with_a_final_version_downloads_the_participation_certificate()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        var workId = await _api.CreateApprovedWorkAsync(eventId, _personId, _professorId);
        await _api.AddFinalVersionAsync(workId);

        var response = await _person.GetAsync(ParticipationPdf(eventId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await response.IsPdfAsync());
    }

    [Fact]
    public async Task A_work_without_its_final_version_gives_no_participation_certificate()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        await _api.CreateApprovedWorkAsync(eventId, _personId, _professorId);

        var response = await _person.GetAsync(ParticipationPdf(eventId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("final version", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task The_final_version_of_somebody_elses_work_does_not_count()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        var other = await _api.CreateUserAsync("otra@feria.test");
        await _api.AddFinalVersionAsync(await _api.CreateApprovedWorkAsync(eventId, other, _professorId));

        Assert.Equal(HttpStatusCode.BadRequest, (await _person.GetAsync(ParticipationPdf(eventId))).StatusCode);
    }

    // ---- "My certificates" ----

    [Fact]
    public async Task My_certificates_lists_what_the_person_qualifies_for_and_when_it_is_available()
    {
        var finished = await _api.CreateEventAsync(_adminId, "Feria 2025", closed: true);
        var ongoing = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: false);
        await _api.RegisterAsync(finished, _personId, attended: true);
        await _api.RegisterAsync(ongoing, _personId, attended: true);
        await _api.AddFinalVersionAsync(await _api.CreateApprovedWorkAsync(finished, _personId, _professorId, "Seguidor solar"));
        (await _person.GetAsync(AttendancePdf(finished))).EnsureSuccessStatusCode();

        var list = (await (await _person.GetAsync("/api/certificates/mine")).JsonAsync()).EnumerateArray().ToList();

        Assert.Equal(3, list.Count);
        var issued = Assert.Single(list, c => c.GetProperty("eventName").GetString() == "Feria 2025" && c.GetProperty("type").GetString() == "Attendance");
        Assert.True(issued.GetProperty("available").GetBoolean());
        Assert.StartsWith("FA-", issued.GetProperty("code").GetString());
        var participation = Assert.Single(list, c => c.GetProperty("type").GetString() == "Participation");
        Assert.Equal("Seguidor solar", participation.GetProperty("workTitle").GetString());
        Assert.True(participation.GetProperty("available").GetBoolean());
        Assert.Null(participation.GetProperty("code").GetString());
        var notYet = Assert.Single(list, c => c.GetProperty("eventName").GetString() == "Feria 2026");
        Assert.False(notYet.GetProperty("available").GetBoolean());
    }

    [Fact]
    public async Task My_certificates_only_shows_my_own()
    {
        var eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: true);
        var other = await _api.CreateUserAsync("otra@feria.test");
        await _api.RegisterAsync(eventId, other, attended: true);

        var list = await (await _person.GetAsync("/api/certificates/mine")).JsonAsync();

        Assert.Empty(list.EnumerateArray());
    }

    [Fact]
    public async Task Without_a_session_certificates_are_refused()
    {
        var anonymous = _api.NewClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/certificates/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(AttendancePdf(Guid.NewGuid()))).StatusCode);
    }
}
