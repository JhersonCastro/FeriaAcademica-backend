using System.Net;
using System.Net.Http.Json;
using FeriaAcademica.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FeriaAcademica.Api.Tests;

/// <summary>The demo data loads on an empty database, can be walked through the real API, and is not loaded twice.</summary>
public class DemoDataApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new(seedDemoData: true);

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static string Email(string local) => $"{local}@{DemoDataSeeder.EmailDomain}";
    private Task<HttpClient> LoginAsync(string local) => _api.LoginAsync(Email(local), DemoDataSeeder.DefaultPassword);

    [Fact]
    public async Task A_guest_who_attended_the_closed_fair_can_evaluate_its_works()
    {
        var natalia = await LoginAsync("natalia.duarte");

        var pending = await (await natalia.GetAsync("/api/evaluations/pending")).JsonAsync();
        Assert.Equal(4, pending.GetArrayLength());   // the four closed fairs she attended
        var fair = pending.EnumerateArray().Single(f => f.GetProperty("eventName").GetString() == "Feria de Ciencias 2026-I");
        var works = fair.GetProperty("works").EnumerateArray().ToList();
        Assert.Equal(5, works.Count);   // the five admitted works; the rejected one is not evaluable

        Assert.All(works, w => Assert.False(w.GetProperty("answered").GetBoolean()));   // she has not evaluated anything yet

        var eventId = fair.GetProperty("eventId").GetGuid();
        var workId = works[0].GetProperty("workId").GetGuid();
        var url = $"/api/events/{eventId}/evaluation/works/{workId}/answers/me";
        var questions = (await (await natalia.GetAsync(url)).JsonAsync()).GetProperty("questions").EnumerateArray().ToList();
        Assert.Equal(5, questions.Count);

        var answers = questions.Select(q => q.GetProperty("type").GetString() switch
        {
            "Scale" => new { questionId = q.GetProperty("id").GetGuid(), value = (double?)4, text = (string?)null },
            "YesNo" => new { questionId = q.GetProperty("id").GetGuid(), value = (double?)1, text = (string?)null },
            _ => new { questionId = q.GetProperty("id").GetGuid(), value = (double?)null, text = (string?)"Muy buen trabajo." },
        });
        Assert.Equal(HttpStatusCode.NoContent, (await natalia.PutAsJsonAsync(url, new { answers })).StatusCode);

        var after = await (await natalia.GetAsync("/api/evaluations/pending")).JsonAsync();
        Assert.Equal(1, after.EnumerateArray().Single(f => f.GetProperty("eventName").GetString() == "Feria de Ciencias 2026-I").GetProperty("works").EnumerateArray().Count(w => w.GetProperty("answered").GetBoolean()));
    }

    [Fact]
    public async Task The_teacher_has_a_submitted_work_waiting_in_the_open_fair()
    {
        var laura = await LoginAsync("laura.gomez");

        var assigned = await (await laura.GetAsync("/api/reviews/assigned")).JsonAsync();
        var waiting = assigned.EnumerateArray().Single(w => w.GetProperty("title").GetString() == "Sistema de riego inteligente con IoT");

        var workId = waiting.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await laura.PostAsync($"/api/reviews/{workId}/start", null)).StatusCode);
        var decision = await laura.PostAsJsonAsync($"/api/reviews/{workId}/decision", new { decision = "Approved", feedback = "Listo." });
        Assert.Equal(HttpStatusCode.NoContent, decision.StatusCode);
    }

    [Fact]
    public async Task Students_the_super_admin_and_the_files_all_work()
    {
        var valentina = await LoginAsync("valentina.torres");
        var mine = await (await valentina.GetAsync("/api/works/mine")).JsonAsync();
        Assert.Equal(3, mine.GetArrayLength());   // two closed fairs and a draft in an open one

        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        var events = await admin.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, events.StatusCode);
        Assert.Contains("Feria de Arte y Humanidades 2027", await events.Content.ReadAsStringAsync());

        // The final version of an admitted work is a real PDF that its author can download.
        var closedWork = mine.EnumerateArray().Single(w => w.GetProperty("title").GetString()!.StartsWith("Biofiltros"));
        var workId = closedWork.GetProperty("id").GetGuid();
        var work = await (await valentina.GetAsync($"/api/works/{workId}")).JsonAsync();
        var fileUrl = work.GetProperty("artifacts").EnumerateArray().First(a => a.GetProperty("fileUrl").ValueKind == System.Text.Json.JsonValueKind.String).GetProperty("fileUrl").GetString();
        Assert.True(await (await valentina.GetAsync(fileUrl)).IsPdfAsync());
    }

    [Fact]
    public async Task Starting_again_does_not_load_the_demo_twice()
    {
        await LoginAsync("valentina.torres");   // the API has started and seeded

        await _api.Services.SeedDemoDataAsync(_api.Services.GetRequiredService<IConfiguration>());

        var (institutions, events) = await _api.Arrange(async (db, _) =>
            (await db.Institutions.CountAsync(i => i.Name == DemoDataSeeder.InstitutionName), await db.Events.CountAsync()));
        Assert.Equal(1, institutions);
        Assert.Equal(11, events);
    }
}
