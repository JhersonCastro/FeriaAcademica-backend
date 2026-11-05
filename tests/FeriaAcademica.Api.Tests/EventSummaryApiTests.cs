using System.Net;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-031: the administrator gets the event's consolidated figures, up to date when asked for.</summary>
public class EventSummaryApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private Guid _adminId, _professorId, _eventId;
    private HttpClient _admin = default!;

    public async Task InitializeAsync()
    {
        _admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        _adminId = await _admin.MyIdAsync();
        _professorId = await _api.CreateUserAsync("profesor@feria.test", "Profesor");
        _eventId = await _api.CreateEventAsync(_adminId, "Feria 2026", closed: false);
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private string Url => $"/api/events/{_eventId}/report/summary";

    private async Task<Guid> Author() => await _api.CreateUserAsync($"autor-{Guid.NewGuid():N}@feria.test");

    private async Task AddWork(Action<Domain.Works.Work, DateTime>? reach = null) =>
        await _api.CreateReviewedWorkAsync(_eventId, await Author(), _professorId, reach);

    [Fact]
    public async Task It_consolidates_the_works_of_the_event_by_their_outcome()
    {
        await AddWork((w, now) => w.Admit(_professorId, now));
        await AddWork((w, now) => w.Admit(_professorId, now));
        await AddWork((w, now) => w.RejectDefinitively(_professorId, "Fuera de alcance.", now));
        await AddWork((w, now) => w.Reject(_professorId, "Falta la metodología.", now));
        await AddWork();

        var summary = await (await _admin.GetAsync(Url)).JsonAsync();

        Assert.Equal("Feria 2026", summary.GetProperty("eventName").GetString());
        Assert.Equal(5, summary.GetProperty("worksReceived").GetInt32());
        Assert.Equal(2, summary.GetProperty("worksApproved").GetInt32());
        Assert.Equal(1, summary.GetProperty("worksRejected").GetInt32());
        Assert.Equal(1, summary.GetProperty("worksReturnedForChanges").GetInt32());
        Assert.Equal(1, summary.GetProperty("worksInReview").GetInt32());
    }

    [Fact]
    public async Task A_draft_that_was_never_submitted_is_not_a_work_received()
    {
        await _api.CreateDraftWorkAsync(_eventId, await Author(), _professorId);

        var summary = await (await _admin.GetAsync(Url)).JsonAsync();

        Assert.Equal(0, summary.GetProperty("worksReceived").GetInt32());
    }

    [Fact]
    public async Task It_counts_the_registered_people_and_those_who_attended()
    {
        await _api.RegisterAsync(_eventId, await Author(), attended: true);
        await _api.RegisterAsync(_eventId, await Author(), attended: true);
        await _api.RegisterAsync(_eventId, await Author(), attended: false);

        var summary = await (await _admin.GetAsync(Url)).JsonAsync();

        Assert.Equal(3, summary.GetProperty("registered").GetInt32());
        Assert.Equal(2, summary.GetProperty("attended").GetInt32());
    }

    [Fact]
    public async Task It_reflects_the_data_at_the_moment_it_is_asked_for()
    {
        var before = await (await _admin.GetAsync(Url)).JsonAsync();
        Assert.Equal(0, before.GetProperty("worksApproved").GetInt32());

        await AddWork((w, now) => w.Admit(_professorId, now));
        await _api.RegisterAsync(_eventId, await Author(), attended: true);

        var after = await (await _admin.GetAsync(Url)).JsonAsync();
        Assert.Equal(1, after.GetProperty("worksApproved").GetInt32());
        Assert.Equal(1, after.GetProperty("attended").GetInt32());
        Assert.True(after.GetProperty("generatedAt").GetDateTime() >= before.GetProperty("generatedAt").GetDateTime());
    }

    [Fact]
    public async Task Works_of_other_events_do_not_count()
    {
        var other = await _api.CreateEventAsync(_adminId, "Otra feria", closed: false);
        await _api.CreateReviewedWorkAsync(other, await Author(), _professorId);

        var summary = await (await _admin.GetAsync(Url)).JsonAsync();

        Assert.Equal(0, summary.GetProperty("worksReceived").GetInt32());
    }

    [Fact]
    public async Task An_unknown_event_gives_not_found()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.GetAsync($"/api/events/{Guid.NewGuid()}/report/summary")).StatusCode);
    }

    [Fact]
    public async Task Only_the_administrator_can_see_it()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.NewClient().GetAsync(Url)).StatusCode);

        await _api.CreateUserAsync("persona@feria.test");
        var person = await _api.LoginAsync("persona@feria.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await person.GetAsync(Url)).StatusCode);
    }
}
