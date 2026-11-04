using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-032: the administrator composes the event report and downloads it as a PDF.</summary>
public class EventReportApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private Guid _eventId;
    private HttpClient _admin = default!;

    public async Task InitializeAsync()
    {
        _admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await _admin.MyIdAsync(), "Feria Académica 2026", closed: false);
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private Task<HttpResponseMessage> Save(string? html, HttpClient? client = null) =>
        (client ?? _admin).PutAsJsonAsync($"/api/events/{_eventId}/report", new { html });

    [Fact]
    public async Task The_administrator_saves_the_report_and_reads_it_back_as_written()
    {
        const string html = "<h1>Informe final</h1><p>Asistieron <b>120</b> personas.</p>";

        Assert.Equal(HttpStatusCode.NoContent, (await Save(html)).StatusCode);

        var report = await (await _admin.GetAsync($"/api/events/{_eventId}/report")).JsonAsync();
        Assert.Equal(html, report.GetProperty("html").GetString());
        Assert.Equal("Feria Académica 2026", report.GetProperty("eventName").GetString());
    }

    [Fact]
    public async Task Saving_again_replaces_the_content()
    {
        (await Save("<p>primera</p>")).EnsureSuccessStatusCode();
        (await Save("<p>segunda</p>")).EnsureSuccessStatusCode();

        var report = await (await _admin.GetAsync($"/api/events/{_eventId}/report")).JsonAsync();
        Assert.Equal("<p>segunda</p>", report.GetProperty("html").GetString());
    }

    [Fact]
    public async Task The_administrator_downloads_the_report_as_a_PDF()
    {
        (await Save("<h1>Informe final</h1><p>Asistieron <b>120</b> personas.</p><ul><li>Taller</li><li>Feria</li></ul>")).EnsureSuccessStatusCode();

        var response = await _admin.GetAsync($"/api/events/{_eventId}/report/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await response.IsPdfAsync());
        Assert.Equal("informe-feria-academica-2026.pdf", response.Content.Headers.ContentDisposition?.FileName);
    }

    [Fact]
    public async Task The_PDF_follows_the_content_of_the_report()
    {
        (await Save("<p>corto</p>")).EnsureSuccessStatusCode();
        var shortPdf = await (await _admin.GetAsync($"/api/events/{_eventId}/report/pdf")).Content.ReadAsByteArrayAsync();

        (await Save(string.Concat(Enumerable.Repeat("<p>Párrafo con cifras: 1, 2, 3, 4, 5, 6, 7, 8, 9.</p>", 200)))).EnsureSuccessStatusCode();
        var longPdf = await (await _admin.GetAsync($"/api/events/{_eventId}/report/pdf")).Content.ReadAsByteArrayAsync();

        Assert.True(longPdf.Length > shortPdf.Length);
    }

    [Fact]
    public async Task Inline_images_are_kept_in_the_PDF()
    {
        const string pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        (await Save("<p>sin imagen</p>")).EnsureSuccessStatusCode();
        var without = await (await _admin.GetAsync($"/api/events/{_eventId}/report/pdf")).Content.ReadAsByteArrayAsync();

        (await Save($"<p>sin imagen</p><img width=\"200\" height=\"200\" src=\"data:image/png;base64,{pixel}\"/>")).EnsureSuccessStatusCode();
        var response = await _admin.GetAsync($"/api/events/{_eventId}/report/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadAsByteArrayAsync()).Length > without.Length);
    }

    [Theory]
    [InlineData("<p>texto</p><img src=\"http://10.255.255.1/x.png\"/>")]
    [InlineData("<link rel=\"stylesheet\" href=\"http://10.255.255.1/x.css\"/><p>texto</p>")]
    [InlineData("<style>@import url(http://10.255.255.1/x.css);</style><p>texto</p>")]
    [InlineData("<p style=\"background-image:url(http://10.255.255.1/x.png)\">texto</p>")]
    public async Task The_server_never_fetches_remote_resources_named_by_the_report(string html)
    {
        (await Save(html)).EnsureSuccessStatusCode();

        var clock = Stopwatch.StartNew();
        var response = await _admin.GetAsync($"/api/events/{_eventId}/report/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await response.IsPdfAsync());
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), $"the renderer waited for an outside address ({clock.Elapsed.TotalSeconds:0}s)");
    }

    [Fact]
    public async Task The_PDF_of_an_event_without_report_is_refused()
    {
        var response = await _admin.GetAsync($"/api/events/{_eventId}/report/pdf");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not been generated", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Reading_a_report_that_does_not_exist_gives_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.GetAsync($"/api/events/{_eventId}/report")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_empty_report_is_refused(string? html)
    {
        var response = await Save(html);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_report_for_an_unknown_event_is_refused()
    {
        var response = await _admin.PutAsJsonAsync($"/api/events/{Guid.NewGuid()}/report", new { html = "<p>x</p>" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not found", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Only_the_administrator_can_write_read_or_download_the_report()
    {
        (await Save("<p>x</p>")).EnsureSuccessStatusCode();
        await _api.CreateUserAsync("estudiante@feria.test");
        var student = await _api.LoginAsync("estudiante@feria.test");

        Assert.Equal(HttpStatusCode.Forbidden, (await Save("<p>y</p>", student)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/events/{_eventId}/report")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/events/{_eventId}/report/pdf")).StatusCode);

        var anonymous = _api.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/events/{_eventId}/report/pdf")).StatusCode);
    }
}
