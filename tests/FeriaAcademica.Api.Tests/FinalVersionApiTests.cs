using System.Net;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-030: the participant with an approved work uploads its final version as a file.</summary>
public class FinalVersionApiTests : IAsyncLifetime
{
    private static readonly byte[] PdfBytes = "%PDF-1.4\nVersion final\n%%EOF"u8.ToArray();

    private readonly ApiFactory _api = new();
    private Guid _eventId, _professorId, _participantId;
    private HttpClient _participant = default!;

    public async Task InitializeAsync()
    {
        _participantId = await _api.CreateUserAsync("estudiante@feria.test", "Estudiante");
        _professorId = await _api.CreateUserAsync("profesor@feria.test", "Profesor");
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await admin.MyIdAsync(), "Feria 2026", closed: false);
        _participant = await _api.LoginAsync("estudiante@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static MultipartFormDataContent Pdf(string name = "final.pdf", byte[]? bytes = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes ?? PdfBytes), "file", name);
        return form;
    }

    private bool AnyFileStored() => Directory.Exists(_api.StoragePath) && Directory.EnumerateFiles(_api.StoragePath, "*", SearchOption.AllDirectories).Any();

    [Fact]
    public async Task The_participant_of_an_approved_work_uploads_the_final_version_and_it_is_marked_as_definitive()
    {
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);

        var response = await _participant.PostAsync($"/api/works/{workId}/final-version", Pdf());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var work = await (await _participant.GetAsync($"/api/works/{workId}")).JsonAsync();
        var final = Assert.Single(work.GetProperty("artifacts").EnumerateArray(), a => a.GetProperty("isFinal").GetBoolean());
        Assert.StartsWith($"/api/works/{workId}/files/", final.GetProperty("fileUrl").GetString());
        Assert.Equal("Admitted", work.GetProperty("status").GetString());
    }

    [Fact]
    public async Task The_final_version_downloads_with_the_same_content_that_was_sent()
    {
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);
        (await _participant.PostAsync($"/api/works/{workId}/final-version", Pdf())).EnsureSuccessStatusCode();
        var work = await (await _participant.GetAsync($"/api/works/{workId}")).JsonAsync();
        var fileUrl = work.GetProperty("artifacts").EnumerateArray().Single(a => a.GetProperty("isFinal").GetBoolean()).GetProperty("fileUrl").GetString()!;

        var download = await _participant.GetAsync(fileUrl);

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PdfBytes, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task A_work_that_is_not_approved_cannot_receive_a_final_version()
    {
        var workId = await _api.CreateDraftWorkAsync(_eventId, _participantId, _professorId);

        var response = await _participant.PostAsync($"/api/works/{workId}/final-version", Pdf());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("approved", await response.ProblemDetailAsync());
        Assert.False(AnyFileStored());
    }

    [Theory]
    [InlineData("final.docx", 1000, "format")]
    [InlineData("final.pdf", 10 * 1024 * 1024 + 1, "maximum size")]
    [InlineData("final.pdf", 0, "empty")]
    public async Task The_format_and_size_of_the_final_document_are_validated_and_nothing_is_stored(string name, int size, string expected)
    {
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);

        var response = await _participant.PostAsync($"/api/works/{workId}/final-version", Pdf(name, new byte[size]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(expected, await response.ProblemDetailAsync());
        var work = await (await _participant.GetAsync($"/api/works/{workId}")).JsonAsync();
        Assert.DoesNotContain(work.GetProperty("artifacts").EnumerateArray(), a => a.GetProperty("isFinal").GetBoolean());
        Assert.False(AnyFileStored());
    }

    [Fact]
    public async Task The_final_version_can_only_be_delivered_once()
    {
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);
        (await _participant.PostAsync($"/api/works/{workId}/final-version", Pdf())).EnsureSuccessStatusCode();

        var again = await _participant.PostAsync($"/api/works/{workId}/final-version", Pdf("otra.pdf"));

        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Contains("already", await again.ProblemDetailAsync());
        Assert.Single(Directory.EnumerateFiles(_api.StoragePath, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Only_the_author_can_deliver_the_final_version()
    {
        await _api.CreateUserAsync("intruso@feria.test", "Intruso");
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);
        var stranger = await _api.LoginAsync("intruso@feria.test");

        var response = await stranger.PostAsync($"/api/works/{workId}/final-version", Pdf());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("author", await response.ProblemDetailAsync());
        Assert.False(AnyFileStored());
    }

    [Fact]
    public async Task The_final_version_needs_the_file()
    {
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);

        var response = await _participant.PostAsync($"/api/works/{workId}/final-version", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_work_that_does_not_exist_is_reported()
    {
        var response = await _participant.PostAsync($"/api/works/{Guid.NewGuid()}/final-version", Pdf());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not found", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Without_a_session_the_final_version_is_refused()
    {
        var response = await _api.NewClient().PostAsync($"/api/works/{Guid.NewGuid()}/final-version", Pdf());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
