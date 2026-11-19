using System.Net;
using System.Text.Json;

namespace FeriaAcademica.Api.Tests;

/// <summary>The reviewer sees the works they approved, with the final version marked, and is told when it is delivered.</summary>
public class ApprovedWorksApiTests : IAsyncLifetime
{
    private static readonly byte[] PdfBytes = "%PDF-1.4\nFinal\n%%EOF"u8.ToArray();

    private readonly ApiFactory _api = new();
    private Guid _eventId, _professorId, _otherProfessorId, _participantId;
    private HttpClient _professor = default!, _participant = default!, _otherProfessor = default!;

    public async Task InitializeAsync()
    {
        _participantId = await _api.CreateUserAsync("estudiante@feria.test", "Estudiante");
        _professorId = await _api.CreateUserAsync("profesor@feria.test", "Profesor");
        _otherProfessorId = await _api.CreateUserAsync("otro@feria.test", "Otro profesor");
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await admin.MyIdAsync(), "Feria 2026", closed: false);
        _participant = await _api.LoginAsync("estudiante@feria.test");
        _professor = await _api.LoginAsync("profesor@feria.test");
        _otherProfessor = await _api.LoginAsync("otro@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private async Task<JsonElement[]> GetListAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.JsonAsync()).EnumerateArray().ToArray();
    }

    private async Task<Guid> NewParticipantAsync() => await _api.CreateUserAsync($"p{Guid.NewGuid():N}@feria.test");

    [Fact]
    public async Task Only_the_approved_works_of_the_reviewer_are_listed_and_the_pending_list_is_unchanged()
    {
        var approved = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);
        var inReview = await _api.CreateReviewedWorkAsync(_eventId, await NewParticipantAsync(), _professorId);
        await _api.CreateDraftWorkAsync(_eventId, await NewParticipantAsync(), _professorId);
        await _api.CreateApprovedWorkAsync(_eventId, await NewParticipantAsync(), _otherProfessorId);

        var list = await GetListAsync(_professor, "/api/reviews/approved");
        var pending = await GetListAsync(_professor, "/api/reviews/assigned");

        Assert.Equal([approved], list.Select(w => w.GetProperty("id").GetGuid()));
        Assert.Equal("Admitted", list[0].GetProperty("status").GetString());
        Assert.Equal([inReview], pending.Select(w => w.GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task The_final_version_arrives_marked_and_the_other_artifacts_do_not()
    {
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId);
        var before = (await GetListAsync(_professor, "/api/reviews/approved")).Single();
        Assert.False(before.GetProperty("artifacts").EnumerateArray().Single().GetProperty("isFinal").GetBoolean());

        var form = new MultipartFormDataContent { { new ByteArrayContent(PdfBytes), "file", "final.pdf" } };
        (await _participant.PostAsync($"/api/works/{workId}/final-version", form)).EnsureSuccessStatusCode();

        var after = (await GetListAsync(_professor, "/api/reviews/approved")).Single();
        Assert.True(after.GetProperty("artifacts").EnumerateArray().Single().GetProperty("isFinal").GetBoolean());
    }

    [Fact]
    public async Task Delivering_the_final_version_notifies_the_reviewer_and_not_the_student()
    {
        var workId = await _api.CreateApprovedWorkAsync(_eventId, _participantId, _professorId, "Seguidor solar");
        var form = new MultipartFormDataContent { { new ByteArrayContent(PdfBytes), "file", "final.pdf" } };

        (await _participant.PostAsync($"/api/works/{workId}/final-version", form)).EnsureSuccessStatusCode();

        var notice = Assert.Single(await GetListAsync(_professor, "/api/notifications"), n => n.GetProperty("type").GetString() == "FinalVersionSubmitted");
        Assert.Equal("The final version of \"Seguidor solar\" was submitted.", notice.GetProperty("message").GetString());
        Assert.DoesNotContain(await GetListAsync(_participant, "/api/notifications"), n => n.GetProperty("type").GetString() == "FinalVersionSubmitted");
    }

    [Fact]
    public async Task A_user_without_assigned_works_gets_an_empty_list()
    {
        Assert.Empty(await GetListAsync(_otherProfessor, "/api/reviews/approved"));
    }

    [Fact]
    public async Task Without_a_session_the_list_is_refused()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.NewClient().GetAsync("/api/reviews/approved")).StatusCode);
    }
}
