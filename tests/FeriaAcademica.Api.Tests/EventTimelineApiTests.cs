using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FeriaAcademica.Domain.Institutions;

namespace FeriaAcademica.Api.Tests;

/// <summary>T-13: the stages programmed when the fair is created are saved, read back with their order, and edited one at a time.</summary>
public class EventTimelineApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private HttpClient _admin = default!;
    private Guid _institutionId;

    public async Task InitializeAsync()
    {
        _admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _institutionId = await _api.Arrange(async (db, _) =>
        {
            var institution = Institution.Create("Universidad del Cauca");
            db.Add(institution);
            await db.SaveChangesAsync();
            return institution.Id;
        });
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    // SQLite returns the dates without a zone; they are UTC, so read them as UTC whatever the machine's zone is.
    private static DateTime Utc(JsonElement date) =>
        DateTime.Parse(date.GetString()!, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

    private async Task<HttpResponseMessage> Create(object? stages) =>
        await _admin.PostAsJsonAsync("/api/events", new { institutionId = _institutionId, name = "Feria 2026", stages });

    private async Task<Guid> CreateWithTimeline()
    {
        var response = await Create(new[]
        {
            new { type = "Reception", startDate = "2026-11-01T00:00:00Z", endDate = "2026-11-20T23:59:00Z" },
            new { type = "call", startDate = "2026-10-15T08:00:00-05:00", endDate = "2026-10-31T23:59:00Z" },
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.JsonAsync()).GetProperty("id").GetGuid();
    }

    private async Task<JsonElement[]> Stages(Guid eventId, HttpClient? client = null) =>
        (await (await (client ?? _admin).GetAsync($"/api/events/{eventId}/stages")).JsonAsync()).EnumerateArray().ToArray();

    // ---- saved when the fair is created ----

    [Fact]
    public async Task The_stages_programmed_when_creating_the_fair_are_saved_and_read_back_with_their_order()
    {
        var eventId = await CreateWithTimeline();

        var stages = await Stages(eventId);

        Assert.Equal(["Call", "Reception"], stages.Select(s => s.GetProperty("type").GetString()));
        Assert.Equal([2, 4], stages.Select(s => s.GetProperty("order").GetInt32()));
        Assert.Equal(new DateTime(2026, 10, 15, 13, 0, 0, DateTimeKind.Utc), Utc(stages[0].GetProperty("startDate")));
        Assert.Equal(new DateTime(2026, 11, 20, 23, 59, 0, DateTimeKind.Utc), Utc(stages[1].GetProperty("endDate")));
    }

    [Fact]
    public async Task A_fair_created_without_stages_has_an_empty_timeline()
    {
        var response = await Create(null);

        var eventId = (await response.JsonAsync()).GetProperty("id").GetGuid();
        Assert.Empty(await Stages(eventId));
    }

    [Fact]
    public async Task A_repeated_or_unknown_stage_is_refused_and_nothing_is_created()
    {
        var repeated = await Create(new[]
        {
            new { type = "Call", startDate = "2026-10-01T00:00:00Z", endDate = "2026-10-05T00:00:00Z" },
            new { type = "Call", startDate = "2026-10-06T00:00:00Z", endDate = "2026-10-09T00:00:00Z" },
        });
        var unknown = await Create(new[] { new { type = "Party", startDate = "2026-10-01T00:00:00Z", endDate = "2026-10-05T00:00:00Z" } });
        var backwards = await Create(new[] { new { type = "Call", startDate = "2026-10-05T00:00:00Z", endDate = "2026-10-01T00:00:00Z" } });

        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        Assert.Contains("more than once", await repeated.ProblemDetailAsync());
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Contains("Unknown stage type", await unknown.ProblemDetailAsync());
        Assert.Equal(HttpStatusCode.BadRequest, backwards.StatusCode);
        Assert.Empty((await (await _admin.GetAsync("/api/events")).JsonAsync()).EnumerateArray());
    }

    [Fact]
    public async Task Anyone_signed_in_can_read_the_stages_of_a_fair()
    {
        var eventId = await CreateWithTimeline();
        await _api.CreateUserAsync("estudiante@feria.test");
        var student = await _api.LoginAsync("estudiante@feria.test");

        Assert.Equal(2, (await Stages(eventId, student)).Length);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.NewClient().GetAsync($"/api/events/{eventId}/stages")).StatusCode);
    }

    // ---- editing one stage ----

    private Task<HttpResponseMessage> Patch(Guid eventId, string type, object body, HttpClient? client = null) =>
        (client ?? _admin).PatchAsJsonAsync($"/api/events/{eventId}/stages/{type}", body);

    [Fact]
    public async Task Editing_only_the_end_of_a_stage_keeps_its_start()
    {
        var eventId = await CreateWithTimeline();

        var response = await Patch(eventId, "Reception", new { endDate = "2026-11-25T23:59:00Z" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var reception = (await Stages(eventId)).Single(s => s.GetProperty("type").GetString() == "Reception");
        Assert.Equal(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), Utc(reception.GetProperty("startDate")));
        Assert.Equal(new DateTime(2026, 11, 25, 23, 59, 0, DateTimeKind.Utc), Utc(reception.GetProperty("endDate")));
    }

    [Fact]
    public async Task Editing_the_end_of_reception_moves_the_submission_deadline()
    {
        var eventId = await CreateWithTimeline();

        await Patch(eventId, "Reception", new { endDate = "2026-11-25T23:59:00Z" });

        var fair = (await (await _admin.GetAsync("/api/events")).JsonAsync()).EnumerateArray().Single(e => e.GetProperty("id").GetGuid() == eventId);
        Assert.Equal(new DateTime(2026, 11, 25, 23, 59, 0, DateTimeKind.Utc), Utc(fair.GetProperty("submissionDeadline")));
    }

    [Fact]
    public async Task An_edit_that_leaves_the_stage_ending_before_it_starts_is_refused()
    {
        var eventId = await CreateWithTimeline();

        Assert.Equal(HttpStatusCode.BadRequest, (await Patch(eventId, "Reception", new { startDate = "2026-12-01T00:00:00Z" })).StatusCode);
    }

    [Fact]
    public async Task An_edit_without_dates_or_of_a_stage_not_in_the_timeline_is_refused()
    {
        var eventId = await CreateWithTimeline();

        var empty = await Patch(eventId, "Reception", new { });
        var missing = await Patch(eventId, "Evaluation", new { endDate = "2026-12-01T00:00:00Z" });

        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Contains("start date, the end date or both", await empty.ProblemDetailAsync());
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Contains("no Evaluation stage", await missing.ProblemDetailAsync());
    }

    [Fact]
    public async Task Only_the_super_admin_can_edit_a_stage()
    {
        var eventId = await CreateWithTimeline();
        await _api.CreateUserAsync("profesor@feria.test");
        var teacher = await _api.LoginAsync("profesor@feria.test");

        Assert.Equal(HttpStatusCode.Forbidden, (await Patch(eventId, "Reception", new { endDate = "2026-11-25T23:59:00Z" }, teacher)).StatusCode);
    }
}
