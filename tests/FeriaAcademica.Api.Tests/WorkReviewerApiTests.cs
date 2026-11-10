using System.Net;
using System.Net.Http.Json;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Api.Tests;

/// <summary>The professor who invites a student is the reviewer of that student's works; the student does not choose one.</summary>
public class WorkReviewerApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private Guid _eventId, _professorId, _otherProfessorId, _studentId;
    private HttpClient _professor = default!, _otherProfessor = default!, _student = default!;

    public async Task InitializeAsync()
    {
        _professorId = await _api.CreateUserAsync("profesor@feria.test", "Profesor");
        _otherProfessorId = await _api.CreateUserAsync("otro.profesor@feria.test", "Otro profesor");
        _studentId = await _api.CreateUserAsync("estudiante@feria.test", "Estudiante");
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await admin.MyIdAsync(), "Feria 2026", closed: false);
        await _api.GiveRoleAsync(_eventId, _professorId, EventRole.Teacher);
        await _api.GiveRoleAsync(_eventId, _otherProfessorId, EventRole.Teacher);
        _professor = await _api.LoginAsync("profesor@feria.test");
        _otherProfessor = await _api.LoginAsync("otro.profesor@feria.test");
        _student = await _api.LoginAsync("estudiante@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    /// <summary>The professor invites the student, who accepts; returns the participation they get.</summary>
    private async Task<Guid> InviteAndAcceptAsync()
    {
        var invite = await _professor.PostAsJsonAsync($"/api/events/{_eventId}/invitations", new { email = "estudiante@feria.test", role = "Participant" });
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        var token = _api.Emails.Sent.Single(e => e.Kind == "Invitation").Token;

        var accept = await _student.PostAsJsonAsync($"/api/invitations/{token}/accept", new { });
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);

        var participation = await (await _student.GetAsync($"/api/events/{_eventId}/participations/me")).JsonAsync();
        return participation.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task The_work_of_an_invited_student_is_reviewed_by_the_professor_who_invited_them()
    {
        var participationId = await InviteAndAcceptAsync();

        var created = await _student.PostAsJsonAsync("/api/works", new { participationId, title = "Seguidor solar" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var work = await (await _student.GetAsync($"/api/works/{(await created.JsonAsync()).GetProperty("id").GetGuid()}")).JsonAsync();
        Assert.Equal(_professorId, work.GetProperty("professorId").GetGuid());
    }

    [Fact]
    public async Task A_professor_sent_by_the_student_is_ignored()
    {
        var participationId = await InviteAndAcceptAsync();

        var created = await _student.PostAsJsonAsync("/api/works", new { participationId, professorId = _otherProfessorId, title = "Seguidor solar" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var work = await (await _student.GetAsync($"/api/works/{(await created.JsonAsync()).GetProperty("id").GetGuid()}")).JsonAsync();
        Assert.Equal(_professorId, work.GetProperty("professorId").GetGuid());
    }

    [Fact]
    public async Task The_work_shows_up_in_the_queue_of_the_inviting_professor_and_not_in_another_one()
    {
        var participationId = await InviteAndAcceptAsync();
        var workId = (await (await _student.PostAsJsonAsync("/api/works", new { participationId, title = "Seguidor solar" })).JsonAsync()).GetProperty("id").GetGuid();
        var artifact = await _student.PostAsJsonAsync($"/api/works/{workId}/artifacts", new { type = (int)FeriaAcademica.Domain.Works.ArtifactType.Document, linkUrl = "https://example.org/informe" });
        Assert.True(artifact.StatusCode == HttpStatusCode.Created, await artifact.Content.ReadAsStringAsync());
        var submit = await _student.PostAsync($"/api/works/{workId}/submit", null);
        Assert.True(submit.StatusCode == HttpStatusCode.NoContent, await submit.Content.ReadAsStringAsync());

        var mine = await (await _professor.GetAsync("/api/reviews/assigned")).JsonAsync();
        var others = await (await _otherProfessor.GetAsync("/api/reviews/assigned")).JsonAsync();

        Assert.Equal(workId, Assert.Single(mine.EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Empty(others.EnumerateArray());
    }

    [Fact]
    public async Task A_participation_that_did_not_come_from_an_invitation_cannot_create_works()
    {
        var participationId = await _api.Arrange(async (db, _) =>
        {
            var participation = Participation.Create(_eventId, _studentId);
            db.Add(participation);
            await db.SaveChangesAsync();
            return participation.Id;
        });

        var response = await _student.PostAsJsonAsync("/api/works", new { participationId, title = "Seguidor solar" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("no professor", await response.ProblemDetailAsync());
    }
}
