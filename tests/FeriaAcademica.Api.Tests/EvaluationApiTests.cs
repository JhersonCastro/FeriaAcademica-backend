using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-023: the questions of a closed event and the answers of those who attended, per work.</summary>
public class EvaluationApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private Guid _eventId, _workId, _professorId, _authorId;
    private HttpClient _coordinator = default!, _attendee = default!, _author = default!, _outsider = default!;

    public async Task InitializeAsync()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await admin.MyIdAsync(), "Feria 2026", closed: true);

        var coordinatorId = await _api.CreateUserAsync("coordinador@feria.test", "Coordinación");
        await _api.GiveRoleAsync(_eventId, coordinatorId, EventRole.Coordinator);
        _professorId = await _api.CreateUserAsync("profesor@feria.test", "Profesor");
        _authorId = await _api.CreateUserAsync("autor@feria.test", "Autora");
        var attendeeId = await _api.CreateUserAsync("asistente@feria.test", "Asistente");
        await _api.CreateUserAsync("afuera@feria.test", "Afuera");

        await _api.RegisterAsync(_eventId, attendeeId, attended: true);
        await _api.RegisterAsync(_eventId, _authorId, attended: true);
        _workId = await _api.CreateApprovedWorkAsync(_eventId, _authorId, _professorId);

        _coordinator = await _api.LoginAsync("coordinador@feria.test");
        _attendee = await _api.LoginAsync("asistente@feria.test");
        _author = await _api.LoginAsync("autor@feria.test");
        _outsider = await _api.LoginAsync("afuera@feria.test");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private string EvaluationUrl => $"/api/events/{_eventId}/evaluation";
    private string AnswersUrl(Guid? workId = null) => $"{EvaluationUrl}/works/{workId ?? _workId}/answers/me";

    private static object Scale(string text = "Clarity", object? id = null) => new { id, type = "Scale", text, scaleMin = 1, scaleMax = 5 };
    private static object YesNo(string text = "Recommend?", object? id = null) => new { id, type = "YesNo", text, scaleMin = (int?)null, scaleMax = (int?)null };
    private static object Written(string text = "Improve?", object? id = null) => new { id, type = "Text", text, scaleMin = (int?)null, scaleMax = (int?)null };

    private async Task SaveQuestionsAsync(HttpClient who, params object[] questions) =>
        Assert.Equal(HttpStatusCode.NoContent, (await who.PutAsJsonAsync(EvaluationUrl, new { questions })).StatusCode);

    private async Task<JsonElement> GetEvaluationAsync(HttpClient who) => await (await who.GetAsync(EvaluationUrl)).JsonAsync();

    private async Task<Guid[]> QuestionIdsAsync() =>
        (await GetEvaluationAsync(_coordinator)).GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("id").GetGuid()).ToArray();

    private static object Number(Guid id, double value) => new { questionId = id, value, text = (string?)null };
    private static object Words(Guid id, string text) => new { questionId = id, value = (double?)null, text };

    private async Task<HttpResponseMessage> AnswerAsync(HttpClient who, Guid? workId, params object[] answers) =>
        await who.PutAsJsonAsync(AnswersUrl(workId), new { answers });

    // ---- the questions ----

    [Fact]
    public async Task An_event_without_questions_answers_an_empty_list_that_can_be_edited()
    {
        var evaluation = await GetEvaluationAsync(_coordinator);

        Assert.Equal("Feria 2026", evaluation.GetProperty("eventName").GetString());
        Assert.Equal("Closed", evaluation.GetProperty("eventStatus").GetString());
        Assert.True(evaluation.GetProperty("canEdit").GetBoolean());
        Assert.Equal(0, evaluation.GetProperty("answersCount").GetInt32());
        Assert.Empty(evaluation.GetProperty("questions").EnumerateArray());
    }

    [Fact]
    public async Task The_coordinator_saves_the_questions_and_gets_them_back_in_order_with_their_type()
    {
        await SaveQuestionsAsync(_coordinator, Scale("A"), YesNo("B"), Written("C"));

        var evaluation = await GetEvaluationAsync(_coordinator);
        var questions = evaluation.GetProperty("questions").EnumerateArray().ToArray();

        Assert.Equal(["A", "B", "C"], questions.Select(q => q.GetProperty("text").GetString()));
        Assert.Equal(["Scale", "YesNo", "Text"], questions.Select(q => q.GetProperty("type").GetString()));
        Assert.Equal([1, 2, 3], questions.Select(q => q.GetProperty("order").GetInt32()));
        Assert.Equal((1, 5), (questions[0].GetProperty("scaleMin").GetInt32(), questions[0].GetProperty("scaleMax").GetInt32()));
        Assert.Equal(JsonValueKind.Null, questions[1].GetProperty("scaleMin").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, evaluation.GetProperty("updatedAt").ValueKind);
    }

    [Fact]
    public async Task Saving_again_edits_reorders_and_removes_questions()
    {
        await SaveQuestionsAsync(_coordinator, Scale("A"), YesNo("B"), Written("C"));
        var ids = await QuestionIdsAsync();

        await SaveQuestionsAsync(_coordinator, Written("C edited", ids[2]), Scale("A", ids[0]), Scale("New"));

        var questions = (await GetEvaluationAsync(_coordinator)).GetProperty("questions").EnumerateArray().ToArray();
        Assert.Equal(["C edited", "A", "New"], questions.Select(q => q.GetProperty("text").GetString()));
        Assert.Equal([ids[2], ids[0]], questions.Take(2).Select(q => q.GetProperty("id").GetGuid()));
        Assert.DoesNotContain(ids[1], questions.Select(q => q.GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task Saving_an_empty_list_removes_every_question()
    {
        await SaveQuestionsAsync(_coordinator, Scale());

        await SaveQuestionsAsync(_coordinator);

        Assert.Empty((await GetEvaluationAsync(_coordinator)).GetProperty("questions").EnumerateArray());
    }

    [Fact]
    public async Task The_super_admin_manages_the_questions_too()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);

        await SaveQuestionsAsync(admin, Scale());

        Assert.Single((await GetEvaluationAsync(admin)).GetProperty("questions").EnumerateArray());
    }

    [Fact]
    public async Task An_attendee_cannot_read_or_change_the_questions_from_the_management_endpoints()
    {
        var put = await _attendee.PutAsJsonAsync(EvaluationUrl, new { questions = new[] { Scale() } });
        var get = await _attendee.GetAsync(EvaluationUrl);

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, get.StatusCode);
        Assert.Contains("coordinator or teacher", await put.ProblemDetailAsync());
    }

    [Fact]
    public async Task An_unknown_event_is_not_found()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/events/{Guid.NewGuid()}/evaluation")).StatusCode);
    }

    [Fact]
    public async Task An_invalid_question_is_rejected_and_nothing_is_saved()
    {
        var response = await _coordinator.PutAsJsonAsync(EvaluationUrl, new { questions = new object[] { Scale("Fine"), new { id = (object?)null, type = "Scale", text = "Bad scale", scaleMin = 5, scaleMax = 5 } } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("maximum must be greater", await response.ProblemDetailAsync());
        Assert.Empty((await GetEvaluationAsync(_coordinator)).GetProperty("questions").EnumerateArray());
    }

    // ---- pending ----

    [Fact]
    public async Task An_attendee_sees_the_works_of_others_to_evaluate_once_the_event_has_questions()
    {
        Assert.Empty((await (await _attendee.GetAsync("/api/evaluations/pending")).JsonAsync()).EnumerateArray());   // no questions yet
        await SaveQuestionsAsync(_coordinator, Scale(), Written());

        var pending = (await (await _attendee.GetAsync("/api/evaluations/pending")).JsonAsync()).EnumerateArray().ToArray();

        var item = Assert.Single(pending);
        Assert.Equal(_eventId, item.GetProperty("eventId").GetGuid());
        Assert.Equal(2, item.GetProperty("questionsCount").GetInt32());
        var work = Assert.Single(item.GetProperty("works").EnumerateArray());
        Assert.Equal(_workId, work.GetProperty("workId").GetGuid());
        Assert.False(work.GetProperty("answered").GetBoolean());
    }

    [Fact]
    public async Task The_author_does_not_see_their_own_work_and_someone_who_did_not_attend_sees_nothing()
    {
        await SaveQuestionsAsync(_coordinator, Scale());

        var authors = (await (await _author.GetAsync("/api/evaluations/pending")).JsonAsync()).EnumerateArray().Single();
        Assert.Empty(authors.GetProperty("works").EnumerateArray());
        Assert.Empty((await (await _outsider.GetAsync("/api/evaluations/pending")).JsonAsync()).EnumerateArray());
    }

    [Fact]
    public async Task A_work_that_did_not_reach_the_fair_is_not_offered()
    {
        await _api.CreateDraftWorkAsync(_eventId, await _api.CreateUserAsync("borrador@feria.test", "Borrador"), _professorId);
        await SaveQuestionsAsync(_coordinator, Scale());

        var item = (await (await _attendee.GetAsync("/api/evaluations/pending")).JsonAsync()).EnumerateArray().Single();

        Assert.Single(item.GetProperty("works").EnumerateArray());
    }

    // ---- answers ----

    [Fact]
    public async Task An_attendee_answers_a_work_and_can_read_and_change_their_answers()
    {
        await SaveQuestionsAsync(_coordinator, Scale(), YesNo(), Written());
        var ids = await QuestionIdsAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await AnswerAsync(_attendee, null, Number(ids[0], 4), Number(ids[1], 1), Words(ids[2], "  More examples "))).StatusCode);

        var mine = await (await _attendee.GetAsync(AnswersUrl())).JsonAsync();
        Assert.Equal("Seguidor solar", mine.GetProperty("workTitle").GetString());
        Assert.Equal(3, mine.GetProperty("questions").GetArrayLength());
        var answers = mine.GetProperty("answers").EnumerateArray().ToDictionary(a => a.GetProperty("questionId").GetGuid());
        Assert.Equal(4, answers[ids[0]].GetProperty("value").GetDouble());
        Assert.Equal("More examples", answers[ids[2]].GetProperty("text").GetString());
        Assert.NotEqual(JsonValueKind.Null, mine.GetProperty("answeredAt").ValueKind);

        // The second time replaces the first: one answer left, the others are gone.
        Assert.Equal(HttpStatusCode.NoContent, (await AnswerAsync(_attendee, null, Number(ids[0], 2))).StatusCode);
        var again = (await (await _attendee.GetAsync(AnswersUrl())).JsonAsync()).GetProperty("answers").EnumerateArray().ToArray();
        Assert.Equal(2, Assert.Single(again).GetProperty("value").GetDouble());

        var pending = (await (await _attendee.GetAsync("/api/evaluations/pending")).JsonAsync()).EnumerateArray().Single();
        Assert.True(pending.GetProperty("works")[0].GetProperty("answered").GetBoolean());
    }

    [Fact]
    public async Task Before_answering_the_person_gets_the_questions_and_no_answers()
    {
        await SaveQuestionsAsync(_coordinator, Scale());

        var mine = await (await _attendee.GetAsync(AnswersUrl())).JsonAsync();

        Assert.Equal(1, mine.GetProperty("questions").GetArrayLength());
        Assert.Empty(mine.GetProperty("answers").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, mine.GetProperty("answeredAt").ValueKind);
    }

    [Fact]
    public async Task Each_person_has_their_own_answers()
    {
        await SaveQuestionsAsync(_coordinator, Scale());
        var ids = await QuestionIdsAsync();
        await AnswerAsync(_attendee, null, Number(ids[0], 5));

        var otherAuthor = await _api.CreateUserAsync("otra@feria.test", "Otra");
        await _api.RegisterAsync(_eventId, otherAuthor, attended: true);
        var otherWork = await _api.CreateApprovedWorkAsync(_eventId, otherAuthor, _professorId);

        // Somebody else sees none of the attendee's answers.
        var mineOnOther = await (await _author.GetAsync(AnswersUrl(otherWork))).JsonAsync();
        Assert.Empty(mineOnOther.GetProperty("answers").EnumerateArray());
    }

    [Fact]
    public async Task Once_somebody_answered_the_questions_are_fixed()
    {
        await SaveQuestionsAsync(_coordinator, Scale());
        var ids = await QuestionIdsAsync();
        await AnswerAsync(_attendee, null, Number(ids[0], 3));

        var evaluation = await GetEvaluationAsync(_coordinator);
        var response = await _coordinator.PutAsJsonAsync(EvaluationUrl, new { questions = new[] { Scale("Changed", ids[0]) } });

        Assert.False(evaluation.GetProperty("canEdit").GetBoolean());
        Assert.Equal(1, evaluation.GetProperty("answersCount").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("The evaluation can no longer be changed.", await response.ProblemDetailAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(2.5)]
    public async Task An_answer_outside_the_scale_is_rejected(double value)
    {
        await SaveQuestionsAsync(_coordinator, Scale());
        var ids = await QuestionIdsAsync();

        var response = await AnswerAsync(_attendee, null, Number(ids[0], value));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("The value is outside the criterion scale.", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task A_failed_save_keeps_the_previous_answers()
    {
        await SaveQuestionsAsync(_coordinator, Scale(), YesNo());
        var ids = await QuestionIdsAsync();
        await AnswerAsync(_attendee, null, Number(ids[0], 4));

        var response = await AnswerAsync(_attendee, null, Number(ids[0], 5), Number(ids[1], 7));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var answers = (await (await _attendee.GetAsync(AnswersUrl())).JsonAsync()).GetProperty("answers").EnumerateArray().ToArray();
        Assert.Equal(4, Assert.Single(answers).GetProperty("value").GetDouble());
    }

    [Fact]
    public async Task The_author_cannot_evaluate_their_own_work()
    {
        await SaveQuestionsAsync(_coordinator, Scale());
        var ids = await QuestionIdsAsync();

        var response = await AnswerAsync(_author, null, Number(ids[0], 5));

        Assert.Equal("You cannot evaluate your own work.", await response.ProblemDetailAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await _author.GetAsync(AnswersUrl())).StatusCode);
    }

    [Fact]
    public async Task Someone_who_did_not_attend_cannot_evaluate()
    {
        await SaveQuestionsAsync(_coordinator, Scale());
        var ids = await QuestionIdsAsync();

        var response = await AnswerAsync(_outsider, null, Number(ids[0], 5));

        Assert.Equal("Only people who attended the event can evaluate its works.", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Nobody_evaluates_before_the_event_is_closed()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        var openEvent = await _api.CreateEventAsync(await admin.MyIdAsync(), "Feria abierta", closed: false);
        var attendeeId = await _attendee.MyIdAsync();
        await _api.RegisterAsync(openEvent, attendeeId, attended: true);
        var work = await _api.CreateApprovedWorkAsync(openEvent, _authorId, _professorId);

        var response = await _attendee.PutAsJsonAsync($"/api/events/{openEvent}/evaluation/works/{work}/answers/me", new { answers = new[] { Number(Guid.NewGuid(), 1) } });

        Assert.Equal("The evaluation opens when the event is closed.", await response.ProblemDetailAsync());
        Assert.Empty((await (await _attendee.GetAsync("/api/evaluations/pending")).JsonAsync()).EnumerateArray());
    }

    [Fact]
    public async Task A_work_that_did_not_reach_the_fair_cannot_be_evaluated()
    {
        await SaveQuestionsAsync(_coordinator, Scale());
        var ids = await QuestionIdsAsync();
        var draft = await _api.CreateDraftWorkAsync(_eventId, await _api.CreateUserAsync("borrador@feria.test", "Borrador"), _professorId);

        var response = await AnswerAsync(_attendee, draft, Number(ids[0], 5));

        Assert.Equal("This work cannot be evaluated.", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task A_work_of_another_event_is_not_found()
    {
        await SaveQuestionsAsync(_coordinator, Scale());
        var ids = await QuestionIdsAsync();
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        var other = await _api.CreateEventAsync(await admin.MyIdAsync(), "Otra feria", closed: true);
        var work = await _api.CreateApprovedWorkAsync(other, _authorId, _professorId);

        var response = await AnswerAsync(_attendee, work, Number(ids[0], 5));

        Assert.Equal("Work not found.", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task An_event_without_questions_cannot_be_answered()
    {
        var response = await AnswerAsync(_attendee, null, Number(Guid.NewGuid(), 5));

        Assert.Equal("The event has no evaluation.", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task The_endpoints_need_a_signed_in_user()
    {
        var anonymous = _api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(EvaluationUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/evaluations/pending")).StatusCode);
    }
}
