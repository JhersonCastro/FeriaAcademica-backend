using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FeriaAcademica.Api.Tests;

/// <summary>The original name and the size of an uploaded file are kept and returned, and the download uses that name.</summary>
public class ArtifactFileNameApiTests : IAsyncLifetime
{
    private static readonly byte[] PdfBytes = "%PDF-1.4\nHola\n%%EOF"u8.ToArray();

    private readonly ApiFactory _api = new();
    private Guid _eventId, _professorId, _participantId;
    private HttpClient _participant = default!, _professor = default!;

    public async Task InitializeAsync()
    {
        _participantId = await _api.CreateUserAsync("estudiante@feria.test", "Estudiante");
        _professorId = await _api.CreateUserAsync("profesor@feria.test", "Profesor");
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await admin.MyIdAsync(), "Feria 2026", closed: false);
        _participant = await _api.LoginAsync("estudiante@feria.test");
        _professor = await _api.LoginAsync("profesor@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static MultipartFormDataContent Pdf(string name, string? type = null)
    {
        var form = new MultipartFormDataContent { { new ByteArrayContent(PdfBytes), "file", name } };
        if (type is not null) form.Add(new StringContent(type), "type");
        return form;
    }

    private async Task<JsonElement[]> ArtifactsAsync(Guid workId) =>
        (await (await _participant.GetAsync($"/api/works/{workId}")).JsonAsync()).GetProperty("artifacts").EnumerateArray().ToArray();

    [Fact]
    public async Task An_uploaded_file_keeps_its_name_and_size()
    {
        var workId = await _api.CreateDraftWorkAsync(_eventId, _participantId, _professorId);

        (await _participant.PostAsync($"/api/works/{workId}/artifacts/file", Pdf("sistema-riego.pdf", "Document"))).EnsureSuccessStatusCode();

        var file = Assert.Single(await ArtifactsAsync(workId));
        Assert.Equal("sistema-riego.pdf", file.GetProperty("fileName").GetString());
        Assert.Equal(PdfBytes.Length, file.GetProperty("sizeBytes").GetInt64());
    }

    [Fact]
    public async Task The_final_version_keeps_its_name_and_size()
    {
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);

        (await _participant.PostAsync($"/api/works/{workId}/final-version", Pdf("definitivo.pdf"))).EnsureSuccessStatusCode();

        var final = (await ArtifactsAsync(workId)).Single(a => a.GetProperty("isFinal").GetBoolean());
        Assert.Equal("definitivo.pdf", final.GetProperty("fileName").GetString());
        Assert.Equal(PdfBytes.Length, final.GetProperty("sizeBytes").GetInt64());
    }

    [Theory]
    [InlineData(@"..\..\secreto.pdf", "secreto.pdf")]
    [InlineData(@"C:\carpeta\informe.pdf", "informe.pdf")]
    [InlineData("../../etc/informe.pdf", "informe.pdf")]
    public async Task A_path_is_reduced_to_the_file_name(string sent, string expected)
    {
        var workId = await _api.CreateDraftWorkAsync(_eventId, _participantId, _professorId);

        (await _participant.PostAsync($"/api/works/{workId}/artifacts/file", Pdf(sent, "Document"))).EnsureSuccessStatusCode();

        Assert.Equal(expected, Assert.Single(await ArtifactsAsync(workId)).GetProperty("fileName").GetString());
    }

    [Fact]
    public async Task A_link_has_no_name_or_size()
    {
        var workId = await _api.CreateDraftWorkAsync(_eventId, _participantId, _professorId);

        (await _participant.PostAsJsonAsync($"/api/works/{workId}/artifacts", new { type = 3, linkUrl = "https://example.org/demo" })).EnsureSuccessStatusCode();

        var link = Assert.Single(await ArtifactsAsync(workId));
        Assert.Equal(JsonValueKind.Null, link.GetProperty("fileName").ValueKind);
        Assert.Equal(JsonValueKind.Null, link.GetProperty("sizeBytes").ValueKind);
    }

    [Fact]
    public async Task A_file_added_with_an_external_url_keeps_the_name_and_size_that_came_with_it()
    {
        var workId = await _api.CreateDraftWorkAsync(_eventId, _participantId, _professorId);

        (await _participant.PostAsJsonAsync($"/api/works/{workId}/artifacts",
            new { type = 0, fileUrl = "https://files.test/a.pdf", fileName = "a.pdf", fileSizeBytes = 1234 })).EnsureSuccessStatusCode();

        var file = Assert.Single(await ArtifactsAsync(workId));
        Assert.Equal(("a.pdf", 1234L), (file.GetProperty("fileName").GetString(), file.GetProperty("sizeBytes").GetInt64()));
    }

    [Fact]
    public async Task A_file_stored_before_the_name_was_kept_returns_null()
    {
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);   // its artifact is added without name or size

        var old = Assert.Single(await ArtifactsAsync(workId));
        Assert.Equal(JsonValueKind.Null, old.GetProperty("fileName").ValueKind);
        Assert.Equal(JsonValueKind.Null, old.GetProperty("sizeBytes").ValueKind);
    }

    [Fact]
    public async Task The_reviewer_sees_the_name_and_size_in_the_inbox()
    {
        var workId = await _api.CreateDraftWorkAsync(_eventId, _participantId, _professorId);
        (await _participant.PostAsync($"/api/works/{workId}/artifacts/file", Pdf("para-revisar.pdf", "Document"))).EnsureSuccessStatusCode();
        (await _participant.PostAsync($"/api/works/{workId}/submit", null)).EnsureSuccessStatusCode();

        var inbox = (await (await _professor.GetAsync("/api/reviews/assigned")).JsonAsync()).EnumerateArray().Single(w => w.GetProperty("id").GetGuid() == workId);

        var file = inbox.GetProperty("artifacts").EnumerateArray().Single();
        Assert.Equal("para-revisar.pdf", file.GetProperty("fileName").GetString());
        Assert.Equal(PdfBytes.Length, file.GetProperty("sizeBytes").GetInt64());
    }

    [Fact]
    public async Task The_download_uses_the_original_name()
    {
        var workId = await _api.CreateDraftWorkAsync(_eventId, _participantId, _professorId);
        (await _participant.PostAsync($"/api/works/{workId}/artifacts/file", Pdf("sistema-riego.pdf", "Document"))).EnsureSuccessStatusCode();
        var fileUrl = Assert.Single(await ArtifactsAsync(workId)).GetProperty("fileUrl").GetString()!;

        var download = await _participant.GetAsync(fileUrl);

        Assert.Equal("sistema-riego.pdf", download.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }
}
