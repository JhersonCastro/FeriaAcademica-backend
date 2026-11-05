using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Api.Tests;

/// <summary>EP-03: the reviewer's queue, the review decision and the notification it sends to the participant.</summary>
public class ReviewsApiTests : IAsyncLifetime
{
    private const string ProfessorEmail = "profesor@feria.test";
    private const string ParticipantEmail = "estudiante@feria.test";
    private const string StrangerEmail = "intruso@feria.test";

    private readonly ApiFactory _api = new();
    private Guid _eventId, _professorId, _participantId;
    private HttpClient _professor = default!, _participant = default!, _stranger = default!;

    public async Task InitializeAsync()
    {
        _participantId = await _api.CreateUserAsync(ParticipantEmail, "Estudiante");
        _professorId = await _api.CreateUserAsync(ProfessorEmail, "Profesor");
        await _api.CreateUserAsync(StrangerEmail, "Intruso");
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await admin.MyIdAsync(), "Feria 2026", closed: false);
        _professor = await _api.LoginAsync(ProfessorEmail);
        _participant = await _api.LoginAsync(ParticipantEmail);
        _stranger = await _api.LoginAsync(StrangerEmail);
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    /// <summary>A submitted work with one artifact. A person has a single participation per event, so each work of a test needs its own participant.</summary>
    private Task<Guid> CreateSubmittedWorkAsync(Guid participantId, Guid professorId, string title = "Seguidor solar", int maxRejections = 3) => _api.Arrange(async (db, _) =>
    {
        var participation = Participation.Create(_eventId, participantId);
        var now = DateTime.UtcNow;
        var work = Work.Create(participation.Id, professorId, title, "Resumen", maxRejections, participantId, now);
        work.AddArtifact(ArtifactType.Document, "https://files.test/v1.pdf", null, now);
        work.AddArtifact(ArtifactType.Document, "https://files.test/v2.pdf", null, now);
        work.Submit(participantId, now);
        db.AddRange(participation, work);
        await db.SaveChangesAsync();
        return work.Id;
    });

    private Task<Guid> CreateSubmittedWorkAsync(int maxRejections = 3) => CreateSubmittedWorkAsync(_participantId, _professorId, maxRejections: maxRejections);

    private async Task<Guid> CreateWorkInReviewAsync(int maxRejections = 3)
    {
        var workId = await CreateSubmittedWorkAsync(maxRejections);
        (await StartAsync(_professor, workId)).EnsureSuccessStatusCode();
        return workId;
    }

    private static Task<HttpResponseMessage> StartAsync(HttpClient client, Guid workId) => client.PostAsync($"/api/reviews/{workId}/start", null);

    private static Task<HttpResponseMessage> DecideAsync(HttpClient client, Guid workId, string decision, string? feedback = null) =>
        client.PostAsJsonAsync($"/api/reviews/{workId}/decision", new { decision, feedback });

    private async Task<JsonElement> WorkAsync(Guid workId) => await (await _participant.GetAsync($"/api/works/{workId}")).JsonAsync();

    private async Task<string> StatusOfAsync(Guid workId) => (await WorkAsync(workId)).GetProperty("status").GetString()!;

    private static async Task<JsonElement[]> AssignedAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/reviews/assigned");
        response.EnsureSuccessStatusCode();
        return (await response.JsonAsync()).EnumerateArray().ToArray();
    }

    private static async Task<JsonElement[]> NotificationsAsync(HttpClient client, string query = "")
    {
        var response = await client.GetAsync("/api/notifications" + query);
        response.EnsureSuccessStatusCode();
        return (await response.JsonAsync()).EnumerateArray().ToArray();
    }

    // ---- GET /api/reviews/assigned

    [Fact]
    public async Task The_queue_has_only_the_submitted_and_in_review_works_of_the_signed_in_professor()
    {
        var otherProfessorId = await _api.CreateUserAsync("otro.profesor@feria.test", "Otro profesor");
        var authors = new List<Guid>();
        for (var i = 0; i < 4; i++) authors.Add(await _api.CreateUserAsync($"autor{i}@feria.test", $"Autor {i}"));

        var inReview = await CreateSubmittedWorkAsync(authors[0], _professorId, "En revisión");
        var submitted = await CreateSubmittedWorkAsync(authors[1], _professorId, "Enviado");
        await _api.CreateDraftWorkAsync(_eventId, authors[2], _professorId);
        await _api.CreateApprovedWorkAsync(_eventId, authors[3], _professorId);
        var ofAnotherProfessor = await CreateSubmittedWorkAsync(_participantId, otherProfessorId, "De otro profesor");
        (await StartAsync(_professor, inReview)).EnsureSuccessStatusCode();

        var queue = await AssignedAsync(_professor);

        // Oldest in the queue first.
        Assert.Equal([inReview, submitted], queue.Select(w => w.GetProperty("id").GetGuid()));
        Assert.Equal(["InReview", "Submitted"], queue.Select(w => w.GetProperty("status").GetString()));
        Assert.DoesNotContain(queue, w => w.GetProperty("id").GetGuid() == ofAnotherProfessor);
    }

    [Fact]
    public async Task The_queue_shows_the_event_and_only_the_latest_version_of_each_artifact()
    {
        var workId = await CreateSubmittedWorkAsync();

        var work = Assert.Single(await AssignedAsync(_professor));

        Assert.Equal(workId, work.GetProperty("id").GetGuid());
        Assert.Equal(_eventId, work.GetProperty("eventId").GetGuid());
        Assert.Equal("Feria 2026", work.GetProperty("eventName").GetString());
        Assert.Equal("Seguidor solar", work.GetProperty("title").GetString());
        var artifact = Assert.Single(work.GetProperty("artifacts").EnumerateArray());
        Assert.Equal(2, artifact.GetProperty("version").GetInt32());
        Assert.Equal("https://files.test/v2.pdf", artifact.GetProperty("fileUrl").GetString());
    }

    [Fact]
    public async Task A_person_who_reviews_nothing_has_an_empty_queue()
    {
        await CreateSubmittedWorkAsync();

        Assert.Empty(await AssignedAsync(_stranger));
    }

    [Fact]
    public async Task Without_a_session_the_queue_is_refused()
    {
        var response = await _api.NewClient().GetAsync("/api/reviews/assigned");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- POST /api/reviews/{workId}/start

    [Fact]
    public async Task The_assigned_professor_moves_a_submitted_work_to_review()
    {
        var workId = await CreateSubmittedWorkAsync();

        var response = await StartAsync(_professor, workId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("InReview", await StatusOfAsync(workId));
        Assert.Equal("InReview", Assert.Single(await AssignedAsync(_professor)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Another_user_cannot_start_the_review()
    {
        var workId = await CreateSubmittedWorkAsync();

        var response = await StartAsync(_stranger, workId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Only the professor", await response.ProblemDetailAsync());
        Assert.Equal("Submitted", await StatusOfAsync(workId));
    }

    [Fact]
    public async Task A_draft_cannot_enter_review()
    {
        var workId = await _api.CreateDraftWorkAsync(_eventId, _participantId, _professorId);

        var response = await StartAsync(_professor, workId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("submitted", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task A_review_cannot_be_started_twice()
    {
        var workId = await CreateWorkInReviewAsync();

        var response = await StartAsync(_professor, workId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Starting_the_review_of_a_work_that_does_not_exist_is_reported()
    {
        var response = await StartAsync(_professor, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not found", await response.ProblemDetailAsync());
    }

    // ---- POST /api/reviews/{workId}/decision

    [Theory]
    [InlineData("Approved", null, "Admitted", "was approved.")]
    [InlineData("ApprovedWithChanges", "Corrige la metodología", "Draft", "was approved with changes: apply the feedback and submit it again.")]
    [InlineData("Rejected", "No cumple las bases", "RejectedDefinitively", "was rejected.")]
    public async Task The_decision_changes_the_work_and_notifies_the_participant(string decision, string? feedback, string expectedStatus, string expectedOutcome)
    {
        var workId = await CreateWorkInReviewAsync();

        var response = await DecideAsync(_professor, workId, decision, feedback);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(expectedStatus, await StatusOfAsync(workId));
        var notification = Assert.Single(await NotificationsAsync(_participant));
        Assert.Equal("WorkReviewed", notification.GetProperty("type").GetString());
        Assert.Equal($"Your work \"Seguidor solar\" {expectedOutcome}", notification.GetProperty("message").GetString());
        Assert.Equal(_eventId, notification.GetProperty("eventId").GetGuid());
        Assert.False(notification.GetProperty("isRead").GetBoolean());
        Assert.Empty(await NotificationsAsync(_professor));
    }

    [Theory]
    [InlineData("Approved", "Admitted")]
    [InlineData("ApprovedWithChanges", "Rejected")]
    [InlineData("Rejected", "DefinitiveRejection")]
    public async Task The_participant_reads_the_feedback_of_the_decision_on_the_work(string decision, string expectedResult)
    {
        var workId = await CreateWorkInReviewAsync();

        (await DecideAsync(_professor, workId, decision, "  Revisa las conclusiones  ")).EnsureSuccessStatusCode();

        var review = Assert.Single((await WorkAsync(workId)).GetProperty("reviews").EnumerateArray());
        Assert.Equal(expectedResult, review.GetProperty("result").GetString());
        Assert.Equal("Revisa las conclusiones", review.GetProperty("observations").GetString());
    }

    [Theory]
    [InlineData("ApprovedWithChanges", null)]
    [InlineData("ApprovedWithChanges", "   ")]
    [InlineData("Rejected", null)]
    [InlineData("Rejected", "   ")]
    public async Task Feedback_is_required_unless_the_work_is_approved(string decision, string? feedback)
    {
        var workId = await CreateWorkInReviewAsync();

        var response = await DecideAsync(_professor, workId, decision, feedback);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Feedback is required", await response.ProblemDetailAsync());
        Assert.Equal("InReview", await StatusOfAsync(workId));
        Assert.Empty(await NotificationsAsync(_participant));
    }

    [Theory]
    [InlineData("""{ "decision": "Aprobado" }""")]
    [InlineData("""{ "decision": 99 }""")]
    [InlineData("""{ "decision": null, "feedback": "Bien" }""")]
    [InlineData("""{ "feedback": "Bien" }""")]
    [InlineData("{ }")]
    public async Task A_decision_that_is_missing_or_unknown_is_refused_and_does_not_approve_the_work(string body)
    {
        var workId = await CreateWorkInReviewAsync();

        var response = await _professor.PostAsync($"/api/reviews/{workId}/decision", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InReview", await StatusOfAsync(workId));
        Assert.Empty(await NotificationsAsync(_participant));
    }

    [Fact]
    public async Task A_missing_decision_is_explained_to_the_caller()
    {
        var workId = await CreateWorkInReviewAsync();

        var response = await _professor.PostAsJsonAsync($"/api/reviews/{workId}/decision", new { feedback = "Bien" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("must be Approved", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Only_the_assigned_professor_can_decide()
    {
        var workId = await CreateWorkInReviewAsync();

        var byStranger = await DecideAsync(_stranger, workId, "Approved");
        var byAuthor = await DecideAsync(_participant, workId, "Approved");

        Assert.Equal(HttpStatusCode.BadRequest, byStranger.StatusCode);
        Assert.Contains("Only the professor", await byStranger.ProblemDetailAsync());
        Assert.Equal(HttpStatusCode.BadRequest, byAuthor.StatusCode);
        Assert.Equal("InReview", await StatusOfAsync(workId));
        Assert.Empty(await NotificationsAsync(_participant));
    }

    [Fact]
    public async Task A_work_that_was_not_opened_cannot_be_decided()
    {
        var workId = await CreateSubmittedWorkAsync();

        var response = await DecideAsync(_professor, workId, "Approved");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not in review", await response.ProblemDetailAsync());
        Assert.Equal("Submitted", await StatusOfAsync(workId));
    }

    [Fact]
    public async Task A_work_can_only_be_decided_once()
    {
        var workId = await CreateWorkInReviewAsync();
        (await DecideAsync(_professor, workId, "Approved")).EnsureSuccessStatusCode();

        var again = await DecideAsync(_professor, workId, "Rejected", "Cambié de opinión");

        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Equal("Admitted", await StatusOfAsync(workId));
        Assert.Single(await NotificationsAsync(_participant));
    }

    [Fact]
    public async Task A_decided_work_leaves_the_queue()
    {
        var workId = await CreateWorkInReviewAsync();

        (await DecideAsync(_professor, workId, "ApprovedWithChanges", "Corrige la metodología")).EnsureSuccessStatusCode();

        Assert.Empty(await AssignedAsync(_professor));
        Assert.Equal(1, (await WorkAsync(workId)).GetProperty("rejectionCount").GetInt32());
    }

    [Fact]
    public async Task Deciding_a_work_that_does_not_exist_is_reported()
    {
        var response = await DecideAsync(_professor, Guid.NewGuid(), "Approved");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not found", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Without_a_session_the_decision_is_refused()
    {
        var response = await DecideAsync(_api.NewClient(), Guid.NewGuid(), "Approved");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- Rejection limit

    [Fact]
    public async Task When_changes_reach_the_rejection_limit_the_work_closes_and_the_participant_is_told_it_was_rejected()
    {
        var workId = await CreateWorkInReviewAsync(maxRejections: 1);

        var response = await DecideAsync(_professor, workId, "ApprovedWithChanges", "Faltan las conclusiones");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var work = await WorkAsync(workId);
        Assert.Equal("RejectedDefinitively", work.GetProperty("status").GetString());
        Assert.Equal("ByAttempts", work.GetProperty("closure").GetString());
        var notification = Assert.Single(await NotificationsAsync(_participant));
        Assert.Equal("Your work \"Seguidor solar\" was rejected.", notification.GetProperty("message").GetString());
    }

    [Fact]
    public async Task A_work_goes_through_every_attempt_over_http_until_the_rejection_limit_closes_it()
    {
        var workId = await CreateWorkInReviewAsync(maxRejections: 2);

        (await DecideAsync(_professor, workId, "ApprovedWithChanges", "Primera corrección")).EnsureSuccessStatusCode();
        var draft = await WorkAsync(workId);
        Assert.Equal("Draft", draft.GetProperty("status").GetString());
        Assert.Equal(2, draft.GetProperty("currentAttempt").GetInt32());

        // Second attempt: the author submits it again and it goes back to the professor's queue.
        (await _participant.PostAsync($"/api/works/{workId}/submit", null)).EnsureSuccessStatusCode();
        Assert.Equal(2, Assert.Single(await AssignedAsync(_professor)).GetProperty("currentAttempt").GetInt32());
        (await StartAsync(_professor, workId)).EnsureSuccessStatusCode();
        (await DecideAsync(_professor, workId, "ApprovedWithChanges", "Segunda corrección")).EnsureSuccessStatusCode();

        var closed = await WorkAsync(workId);
        Assert.Equal("RejectedDefinitively", closed.GetProperty("status").GetString());
        Assert.Equal("ByAttempts", closed.GetProperty("closure").GetString());
        Assert.Equal(2, closed.GetProperty("rejectionCount").GetInt32());
        Assert.Equal(["Primera corrección", "Segunda corrección"], closed.GetProperty("reviews").EnumerateArray().Select(r => r.GetProperty("observations").GetString()));
        Assert.Empty(await AssignedAsync(_professor));

        // Newest first: the closure is reported as a rejection, the earlier attempt as changes to apply.
        var notifications = await NotificationsAsync(_participant);
        Assert.Equal(2, notifications.Length);
        Assert.EndsWith("was rejected.", notifications[0].GetProperty("message").GetString());
        Assert.Contains("approved with changes", notifications[1].GetProperty("message").GetString());
    }

    [Fact]
    public async Task A_closed_work_cannot_be_submitted_again()
    {
        var workId = await CreateWorkInReviewAsync(maxRejections: 1);
        (await DecideAsync(_professor, workId, "ApprovedWithChanges", "Faltan las conclusiones")).EnsureSuccessStatusCode();

        var response = await _participant.PostAsync($"/api/works/{workId}/submit", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- GET /api/notifications and POST /api/notifications/{id}/read

    /// <summary>Two notifications for the participant: changes requested first, approval afterwards.</summary>
    private async Task<JsonElement[]> TwoNotificationsAsync()
    {
        var workId = await CreateWorkInReviewAsync();
        (await DecideAsync(_professor, workId, "ApprovedWithChanges", "Corrige la metodología")).EnsureSuccessStatusCode();
        (await _participant.PostAsync($"/api/works/{workId}/submit", null)).EnsureSuccessStatusCode();
        (await StartAsync(_professor, workId)).EnsureSuccessStatusCode();
        (await DecideAsync(_professor, workId, "Approved")).EnsureSuccessStatusCode();
        return await NotificationsAsync(_participant);
    }

    [Fact]
    public async Task The_notifications_come_newest_first_and_only_for_their_recipient()
    {
        var notifications = await TwoNotificationsAsync();

        Assert.Equal(2, notifications.Length);
        Assert.EndsWith("was approved.", notifications[0].GetProperty("message").GetString());
        Assert.Contains("approved with changes", notifications[1].GetProperty("message").GetString());
        Assert.Empty(await NotificationsAsync(_stranger));
    }

    [Fact]
    public async Task The_recipient_marks_a_notification_as_read_and_it_leaves_the_unread_list()
    {
        var notifications = await TwoNotificationsAsync();
        var read = notifications[0].GetProperty("id").GetGuid();
        var unread = notifications[1].GetProperty("id").GetGuid();
        Assert.Equal(2, (await NotificationsAsync(_participant, "?unreadOnly=true")).Length);

        var response = await _participant.PostAsync($"/api/notifications/{read}/read", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(unread, Assert.Single(await NotificationsAsync(_participant, "?unreadOnly=true")).GetProperty("id").GetGuid());
        var all = await NotificationsAsync(_participant, "?unreadOnly=false");
        Assert.Equal(2, all.Length);
        Assert.True(Assert.Single(all, n => n.GetProperty("id").GetGuid() == read).GetProperty("isRead").GetBoolean());
    }

    [Fact]
    public async Task Marking_a_notification_as_read_twice_changes_nothing()
    {
        var id = (await TwoNotificationsAsync())[0].GetProperty("id").GetGuid();
        (await _participant.PostAsync($"/api/notifications/{id}/read", null)).EnsureSuccessStatusCode();

        var again = await _participant.PostAsync($"/api/notifications/{id}/read", null);

        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Single(await NotificationsAsync(_participant, "?unreadOnly=true"));
    }

    [Fact]
    public async Task Only_the_recipient_can_mark_a_notification_as_read()
    {
        var id = (await TwoNotificationsAsync())[0].GetProperty("id").GetGuid();

        var byStranger = await _stranger.PostAsync($"/api/notifications/{id}/read", null);
        var byProfessor = await _professor.PostAsync($"/api/notifications/{id}/read", null);

        Assert.Equal(HttpStatusCode.BadRequest, byStranger.StatusCode);
        Assert.Contains("Only the recipient", await byStranger.ProblemDetailAsync());
        Assert.Equal(HttpStatusCode.BadRequest, byProfessor.StatusCode);
        Assert.Equal(2, (await NotificationsAsync(_participant, "?unreadOnly=true")).Length);
    }

    [Fact]
    public async Task Marking_a_notification_that_does_not_exist_is_reported()
    {
        var response = await _participant.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not found", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Without_a_session_the_notifications_are_refused()
    {
        var client = _api.NewClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null)).StatusCode);
    }
}
