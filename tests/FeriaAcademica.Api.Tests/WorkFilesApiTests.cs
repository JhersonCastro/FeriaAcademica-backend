using System.Net;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Api.Tests;

/// <summary>A PDF uploaded to a work can be downloaded by the people who may read it (it used to answer 404 to everyone).</summary>
public class WorkFilesApiTests : IAsyncLifetime
{
    private static readonly byte[] PdfBytes = "%PDF-1.4\nSeguidor solar\n%%EOF"u8.ToArray();

    private readonly ApiFactory _api = new();
    private Guid _workId;
    private HttpClient _author = default!, _professor = default!, _admin = default!, _other = default!;
    private HttpClient _coordinator = default!, _coordinatorElsewhere = default!, _assignedJury = default!, _unassignedJury = default!;

    public async Task InitializeAsync()
    {
        var authorId = await _api.CreateUserAsync("estudiante@feria.test", "Estudiante");
        var professorId = await _api.CreateUserAsync("profesor@feria.test", "Profesor");
        await _api.CreateUserAsync("otro@feria.test", "Otra persona");
        _admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        var adminId = await _admin.MyIdAsync();
        var eventId = await _api.CreateEventAsync(adminId, "Feria 2026", closed: false);
        var otherEventId = await _api.CreateEventAsync(adminId, "Otra feria", closed: false);
        _workId = await _api.CreateDraftWorkAsync(eventId, authorId, professorId);

        var coordinatorId = await _api.CreateUserAsync("coordinador@feria.test", "Coordinador");
        var coordinatorElsewhereId = await _api.CreateUserAsync("coordinador.otra@feria.test", "Coordinador de otra feria");
        var assignedJuryId = await _api.CreateUserAsync("jurado@feria.test", "Jurado asignado");
        var unassignedJuryId = await _api.CreateUserAsync("jurado.libre@feria.test", "Jurado sin asignar");
        await _api.GiveRoleAsync(eventId, coordinatorId, EventRole.Coordinator);
        await _api.GiveRoleAsync(otherEventId, coordinatorElsewhereId, EventRole.Coordinator);
        await _api.GiveRoleAsync(eventId, assignedJuryId, EventRole.Jury);
        await _api.GiveRoleAsync(eventId, unassignedJuryId, EventRole.Jury);
        await _api.Arrange(async (db, _) =>
        {
            db.Add(JuryAssignment.Create(_workId, assignedJuryId));
            await db.SaveChangesAsync();
            return 0;
        });

        _author = await _api.LoginAsync("estudiante@feria.test");
        _professor = await _api.LoginAsync("profesor@feria.test");
        _other = await _api.LoginAsync("otro@feria.test");
        _coordinator = await _api.LoginAsync("coordinador@feria.test");
        _coordinatorElsewhere = await _api.LoginAsync("coordinador.otra@feria.test");
        _assignedJury = await _api.LoginAsync("jurado@feria.test");
        _unassignedJury = await _api.LoginAsync("jurado.libre@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static MultipartFormDataContent Form(string name = "informe.pdf", string type = "Document")
    {
        var form = new MultipartFormDataContent { { new StringContent(type), "type" } };
        form.Add(new ByteArrayContent(PdfBytes), "file", name);
        return form;
    }

    /// <summary>Uploads through the API and returns the <c>fileUrl</c> the work reports for it.</summary>
    private async Task<string> UploadAsync(string name = "informe.pdf")
    {
        var upload = await _author.PostAsync($"/api/works/{_workId}/artifacts/file", Form(name));
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var work = await (await _author.GetAsync($"/api/works/{_workId}")).JsonAsync();
        return work.GetProperty("artifacts").EnumerateArray().MaxBy(a => a.GetProperty("version").GetInt32()).GetProperty("fileUrl").GetString()!;
    }

    [Fact]
    public async Task The_author_downloads_the_pdf_they_uploaded()
    {
        var fileUrl = await UploadAsync();

        var response = await _author.GetAsync(fileUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PdfBytes, await response.Content.ReadAsByteArrayAsync());
        Assert.Equal("document-v1.pdf", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }

    [Fact]
    public async Task The_reviewer_and_the_super_admin_download_it_too()
    {
        var fileUrl = await UploadAsync();

        Assert.Equal(HttpStatusCode.OK, (await _professor.GetAsync(fileUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _admin.GetAsync(fileUrl)).StatusCode);
    }

    [Fact]
    public async Task The_coordinator_of_the_event_and_the_jury_assigned_to_the_work_download_it_too()
    {
        var fileUrl = await UploadAsync();

        Assert.Equal(HttpStatusCode.OK, (await _coordinator.GetAsync(fileUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _assignedJury.GetAsync(fileUrl)).StatusCode);
    }

    [Fact]
    public async Task A_coordinator_of_another_event_and_a_jury_member_not_assigned_to_the_work_get_404()
    {
        var fileUrl = await UploadAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await _coordinatorElsewhere.GetAsync(fileUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _unassignedJury.GetAsync(fileUrl)).StatusCode);
    }

    [Fact]
    public async Task Someone_else_gets_404_as_if_the_file_did_not_exist()
    {
        var fileUrl = await UploadAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await _other.GetAsync(fileUrl)).StatusCode);
    }

    [Fact]
    public async Task Downloading_requires_a_session()
    {
        var fileUrl = await UploadAsync();

        var response = await _api.NewClient().GetAsync(fileUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_file_that_was_never_uploaded_is_404()
    {
        var response = await _author.GetAsync($"/api/works/{_workId}/files/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_new_upload_is_a_new_version_and_each_one_keeps_its_own_file()
    {
        var first = await UploadAsync();
        var second = await UploadAsync("informe-v2.pdf");

        Assert.NotEqual(first, second);
        Assert.Equal("document-v2.pdf", (await _author.GetAsync(second)).Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal(HttpStatusCode.OK, (await _author.GetAsync(first)).StatusCode);
    }
}
