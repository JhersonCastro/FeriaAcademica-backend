using System.Net;
using System.Net.Http.Json;

namespace FeriaAcademica.Api.Tests;

/// <summary>The super admin manages institutions; events can only be created in active ones.</summary>
public class InstitutionsApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private HttpClient _admin = default!;

    public async Task InitializeAsync() => _admin = await _api.LoginAsync(ApiFactory.AdminEmail);

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private async Task<Guid> CreateAsync(string name, string? acronym = null)
    {
        var response = await _admin.PostAsJsonAsync("/api/institutions", new { name, acronym });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.JsonAsync()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Only_the_super_admin_manages_institutions()
    {
        await _api.CreateUserAsync("ana@feria.test", "Ana");
        var ana = await _api.LoginAsync("ana@feria.test");
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await ana.GetAsync("/api/institutions")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ana.PostAsJsonAsync("/api/institutions", new { name = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ana.PutAsJsonAsync($"/api/institutions/{id}", new { name = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ana.PostAsync($"/api/institutions/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ana.PostAsync($"/api/institutions/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.NewClient().GetAsync("/api/institutions")).StatusCode);
    }

    [Fact]
    public async Task Without_institutions_the_list_is_empty()
    {
        var list = await (await _admin.GetAsync("/api/institutions")).JsonAsync();

        Assert.Empty(list.EnumerateArray());
    }

    [Fact]
    public async Task The_list_has_every_institution_by_name_with_its_event_count()
    {
        var withEvents = await CreateAsync("Universidad del Cauca", "Unicauca");
        await CreateAsync("Colegio Mayor");
        await _api.CreateUserAsync("ana@feria.test", "Ana");
        var adminId = await _admin.MyIdAsync();
        await _api.Arrange(async (db, _) =>
        {
            db.Add(FeriaAcademica.Domain.Events.AcademicEvent.Create(withEvents, "Feria 1", adminId));
            db.Add(FeriaAcademica.Domain.Events.AcademicEvent.Create(withEvents, "Feria 2", adminId));
            await db.SaveChangesAsync();
            return 0;
        });
        await _admin.PostAsync($"/api/institutions/{await CreateAsync("Zeta")}/deactivate", null);

        var list = (await (await _admin.GetAsync("/api/institutions")).JsonAsync()).EnumerateArray().ToList();

        Assert.Equal(new[] { "Colegio Mayor", "Universidad del Cauca", "Zeta" }, list.Select(i => i.GetProperty("name").GetString()));
        Assert.Equal(JsonValueKindNull, list[0].GetProperty("acronym").ValueKind);
        Assert.Equal("Unicauca", list[1].GetProperty("acronym").GetString());
        Assert.Equal(new[] { 0, 2, 0 }, list.Select(i => i.GetProperty("eventCount").GetInt32()));
        Assert.Equal(new[] { true, true, false }, list.Select(i => i.GetProperty("isActive").GetBoolean()));
    }

    private const System.Text.Json.JsonValueKind JsonValueKindNull = System.Text.Json.JsonValueKind.Null;

    [Fact]
    public async Task A_new_institution_is_active_and_its_name_is_trimmed()
    {
        await CreateAsync("  Universidad del Cauca ", "");

        var item = (await (await _admin.GetAsync("/api/institutions")).JsonAsync()).EnumerateArray().Single();
        Assert.Equal("Universidad del Cauca", item.GetProperty("name").GetString());
        Assert.Equal(JsonValueKindNull, item.GetProperty("acronym").ValueKind);
        Assert.True(item.GetProperty("isActive").GetBoolean());
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public async Task An_empty_name_is_rejected(string name, string? acronym)
    {
        var response = await _admin.PostAsJsonAsync("/api/institutions", new { name, acronym });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("required", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task A_name_or_acronym_that_is_too_long_is_rejected()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _admin.PostAsJsonAsync("/api/institutions", new { name = new string('a', 201) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _admin.PostAsJsonAsync("/api/institutions", new { name = "U", acronym = new string('a', 31) })).StatusCode);
    }

    [Fact]
    public async Task A_duplicate_name_is_rejected_without_telling_case_apart()
    {
        await CreateAsync("Universidad del Cauca");

        var response = await _admin.PostAsJsonAsync("/api/institutions", new { name = "UNIVERSIDAD DEL CAUCA" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("already exists", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Editing_changes_the_name_and_acronym_and_keeps_the_status()
    {
        var id = await CreateAsync("Universidad", "U");
        await _admin.PostAsync($"/api/institutions/{id}/deactivate", null);

        var response = await _admin.PutAsJsonAsync($"/api/institutions/{id}", new { name = "Universidad del Cauca", acronym = "Unicauca" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var item = (await (await _admin.GetAsync("/api/institutions")).JsonAsync()).EnumerateArray().Single();
        Assert.Equal(id, item.GetProperty("id").GetGuid());
        Assert.Equal("Universidad del Cauca", item.GetProperty("name").GetString());
        Assert.Equal("Unicauca", item.GetProperty("acronym").GetString());
        Assert.False(item.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task An_institution_can_keep_its_own_name_when_editing_but_not_take_another_ones()
    {
        var id = await CreateAsync("Universidad");
        await CreateAsync("Colegio");

        Assert.Equal(HttpStatusCode.NoContent, (await _admin.PutAsJsonAsync($"/api/institutions/{id}", new { name = "UNIVERSIDAD", acronym = "U" })).StatusCode);
        var clash = await _admin.PutAsJsonAsync($"/api/institutions/{id}", new { name = "colegio" });
        Assert.Equal(HttpStatusCode.BadRequest, clash.StatusCode);
        Assert.Contains("already exists", await clash.ProblemDetailAsync());
    }

    [Fact]
    public async Task An_unknown_institution_is_not_found()
    {
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await _admin.PutAsJsonAsync($"/api/institutions/{id}", new { name = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.PostAsync($"/api/institutions/{id}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.PostAsync($"/api/institutions/{id}/activate", null)).StatusCode);
    }

    [Fact]
    public async Task An_institution_is_deactivated_and_activated_and_repeating_either_is_rejected()
    {
        var id = await CreateAsync("Universidad");

        var again = await _admin.PostAsync($"/api/institutions/{id}/activate", null);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Contains("already active", await again.ProblemDetailAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await _admin.PostAsync($"/api/institutions/{id}/deactivate", null)).StatusCode);
        var twice = await _admin.PostAsync($"/api/institutions/{id}/deactivate", null);
        Assert.Equal(HttpStatusCode.BadRequest, twice.StatusCode);
        Assert.Contains("already inactive", await twice.ProblemDetailAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await _admin.PostAsync($"/api/institutions/{id}/activate", null)).StatusCode);
    }

    [Fact]
    public async Task An_event_is_created_in_an_active_institution()
    {
        var id = await CreateAsync("Universidad");

        var response = await _admin.PostAsJsonAsync("/api/events", new { institutionId = id, name = "Feria" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task An_event_cannot_be_created_in_an_unknown_institution()
    {
        var response = await _admin.PostAsJsonAsync("/api/events", new { institutionId = Guid.NewGuid(), name = "Feria" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("does not exist", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task An_event_cannot_be_created_in_an_inactive_institution()
    {
        var id = await CreateAsync("Universidad");
        await _admin.PostAsync($"/api/institutions/{id}/deactivate", null);

        var response = await _admin.PostAsJsonAsync("/api/events", new { institutionId = id, name = "Feria" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("inactive", await response.ProblemDetailAsync());
    }
}
