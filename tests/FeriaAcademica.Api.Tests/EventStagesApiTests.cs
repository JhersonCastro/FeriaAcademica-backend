using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Api.Tests;

/// <summary>The frontend lists the stages of a fair and takes them out of its timeline.</summary>
public class EventStagesApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private HttpClient _admin = default!;
    private Guid _eventId;

    public async Task InitializeAsync()
    {
        _admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _eventId = await _api.CreateEventAsync(await _admin.MyIdAsync(), "Feria 2026", closed: false);   // published, with a Registration stage
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private string Stages => $"/api/events/{_eventId}/stages";

    private Task<HttpResponseMessage> Schedule(StageType type, string start, string end) =>
        _admin.PutAsJsonAsync($"{Stages}/{type}", new { startDate = start, endDate = end });

    // SQLite returns the dates without a zone; they are UTC, so read them as UTC whatever the machine's zone is.
    private static DateTime Utc(JsonElement date) =>
        DateTime.Parse(date.GetString()!, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

    private async Task<JsonElement[]> ListAsync() => (await (await _admin.GetAsync(Stages)).JsonAsync()).EnumerateArray().ToArray();

    [Fact]
    public async Task It_lists_the_stages_in_date_order_with_UTC_dates()
    {
        await Schedule(StageType.Reception, "2026-11-01T00:00:00Z", "2026-11-20T23:59:00Z");
        await Schedule(StageType.Call, "2026-10-15T08:00:00-05:00", "2026-10-31T23:59:00Z");

        var stages = await ListAsync();

        // Registration was added without dates by the test setup, so it comes last.
        Assert.Equal(["Call", "Reception", "Registration"], stages.Select(s => s.GetProperty("type").GetString()));
        Assert.Equal("Scheduled", stages[0].GetProperty("status").GetString());
        Assert.Equal(new DateTime(2026, 10, 15, 13, 0, 0, DateTimeKind.Utc), Utc(stages[0].GetProperty("startDate")));   // 08:00 at -05:00
    }

    [Fact]
    public async Task A_stage_without_dates_is_listed_after_the_dated_ones_with_null_dates()
    {
        await Schedule(StageType.Reception, "2026-11-01T00:00:00Z", "2026-11-20T23:59:00Z");

        var stages = await ListAsync();

        Assert.Equal(["Reception", "Registration"], stages.Select(s => s.GetProperty("type").GetString()));
        Assert.Equal(JsonValueKind.Null, stages[1].GetProperty("startDate").ValueKind);
    }

    [Fact]
    public async Task Removing_a_stage_takes_it_out_of_the_list_and_scheduling_it_again_brings_it_back()
    {
        await Schedule(StageType.Reception, "2026-11-01T00:00:00Z", "2026-11-20T23:59:00Z");

        Assert.Equal(HttpStatusCode.NoContent, (await _admin.DeleteAsync($"{Stages}/Reception")).StatusCode);
        Assert.DoesNotContain("Reception", (await ListAsync()).Select(s => s.GetProperty("type").GetString()));

        await Schedule(StageType.Reception, "2026-11-02T00:00:00Z", "2026-11-25T23:59:00Z");
        var back = (await ListAsync()).Single(s => s.GetProperty("type").GetString() == "Reception");
        Assert.Equal(new DateTime(2026, 11, 25, 23, 59, 0, DateTimeKind.Utc), Utc(back.GetProperty("endDate")));
    }

    [Fact]
    public async Task The_last_active_stage_of_a_published_event_cannot_be_removed()
    {
        var response = await _admin.DeleteAsync($"{Stages}/Registration");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("at least one active stage", await response.ProblemDetailAsync());
        Assert.Single(await ListAsync());
    }

    [Fact]
    public async Task Removing_a_stage_that_is_not_in_the_timeline_is_refused()
    {
        var response = await _admin.DeleteAsync($"{Stages}/Evaluation");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_event_is_a_404_on_the_list()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.GetAsync($"/api/events/{Guid.NewGuid()}/stages")).StatusCode);
    }

    [Fact]
    public async Task Only_the_super_admin_can_list_or_remove_stages_and_it_needs_a_session()
    {
        await _api.CreateUserAsync("profesor@feria.test");
        var teacher = await _api.LoginAsync("profesor@feria.test");
        var anonymous = _api.NewClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync(Stages)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.DeleteAsync($"{Stages}/Registration")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Stages)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync($"{Stages}/Registration")).StatusCode);
    }

    [Fact]
    public async Task Removing_Reception_takes_away_the_submission_deadline()
    {
        await Schedule(StageType.Reception, "2026-11-01T00:00:00Z", "2026-11-20T23:59:00Z");
        var before = (await (await _admin.GetAsync("/api/events")).JsonAsync()).EnumerateArray().Single().GetProperty("submissionDeadline");
        Assert.NotEqual(JsonValueKind.Null, before.ValueKind);

        await _admin.DeleteAsync($"{Stages}/Reception");

        var after = (await (await _admin.GetAsync("/api/events")).JsonAsync()).EnumerateArray().Single().GetProperty("submissionDeadline");
        Assert.Equal(JsonValueKind.Null, after.ValueKind);
    }
}
