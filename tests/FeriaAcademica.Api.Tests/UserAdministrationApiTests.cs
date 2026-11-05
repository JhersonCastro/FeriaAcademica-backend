using System.Net;
using System.Net.Http.Json;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-070 and HU-071: the super admin creates, edits, deactivates and lists users.</summary>
public class UserAdministrationApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private HttpClient _admin = default!;

    public async Task InitializeAsync() => _admin = await _api.LoginAsync(ApiFactory.AdminEmail);

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private async Task<Guid> CreateAsync(string email, string fullName = "Persona")
    {
        var response = await _admin.PostAsJsonAsync("/api/users", new { email, fullName });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.JsonAsync()).GetProperty("id").GetGuid();
    }

    private static object Update(string email, string fullName = "Persona") => new { email, fullName };

    // ---- Who can do what ----

    [Fact]
    public async Task Only_the_super_admin_manages_users()
    {
        await _api.CreateUserAsync("ana@feria.test", "Ana");
        var ana = await _api.LoginAsync("ana@feria.test");

        Assert.Equal(HttpStatusCode.Forbidden, (await ana.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ana.PostAsJsonAsync("/api/users", new { email = "x@feria.test", fullName = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.NewClient().GetAsync("/api/users")).StatusCode);
    }

    // ---- Create ----

    [Fact]
    public async Task A_new_user_is_pending_and_receives_a_link_to_set_the_password()
    {
        var id = await CreateAsync("nuevo@feria.test", "Nuevo");

        var user = await (await _admin.GetAsync($"/api/users/{id}")).JsonAsync();
        Assert.Equal("Pending", user.GetProperty("status").GetString());
        var link = Assert.Single(_api.Emails.Sent);
        Assert.Equal("SetPassword", link.Kind);
        Assert.Equal("nuevo@feria.test", link.Email);
    }

    [Fact]
    public async Task A_duplicate_email_is_rejected_when_creating_without_telling_case_apart()
    {
        await CreateAsync("nuevo@feria.test");

        var response = await _admin.PostAsJsonAsync("/api/users", new { email = "NUEVO@feria.test", fullName = "Otro" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("already exists", await response.ProblemDetailAsync());
        Assert.Single(_api.Emails.Sent);
    }

    [Theory]
    [InlineData("", "Nombre", "Email is required")]
    [InlineData("no-es-un-correo", "Nombre", "not valid")]
    [InlineData("ok@feria.test", "", "Full name is required")]
    public async Task The_email_and_the_name_are_validated(string email, string fullName, string expected)
    {
        var response = await _admin.PostAsJsonAsync("/api/users", new { email, fullName });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(expected, await response.ProblemDetailAsync());
    }

    // ---- Edit ----

    [Fact]
    public async Task The_email_of_a_user_cannot_be_changed_to_one_that_belongs_to_someone_else()
    {
        await CreateAsync("uno@feria.test");
        var two = await CreateAsync("dos@feria.test");

        var response = await _admin.PutAsJsonAsync($"/api/users/{two}", Update("uno@feria.test"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("already exists", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task Editing_a_user_keeps_their_own_email_and_updates_the_profile()
    {
        var id = await CreateAsync("uno@feria.test", "Uno");

        var response = await _admin.PutAsJsonAsync($"/api/users/{id}", new { email = "uno@feria.test", fullName = "Uno Editado", phoneNumber = "+57 300 123 4567" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var user = await (await _admin.GetAsync($"/api/users/{id}")).JsonAsync();
        Assert.Equal("Uno Editado", user.GetProperty("fullName").GetString());
        Assert.Equal("+57 300 123 4567", user.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task A_user_edits_their_own_data_but_not_their_email()
    {
        await _api.CreateUserAsync("ana@feria.test", "Ana");
        var ana = await _api.LoginAsync("ana@feria.test");

        var response = await ana.PutAsJsonAsync("/api/users/me", new { fullName = "Ana María" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var me = await (await ana.GetAsync("/api/auth/me")).JsonAsync();
        Assert.Equal("Ana María", me.GetProperty("fullName").GetString());
        Assert.Equal("ana@feria.test", me.GetProperty("email").GetString());
    }

    // ---- Deactivate and activate ----

    [Fact]
    public async Task A_deactivated_user_cannot_sign_in_and_an_activated_one_can_again()
    {
        var id = await _api.CreateUserAsync("ana@feria.test", "Ana");

        (await _admin.PostAsync($"/api/users/{id}/deactivate", null)).EnsureSuccessStatusCode();
        var blocked = await _api.NewClient().PostAsJsonAsync("/api/auth/login", new { email = "ana@feria.test", password = ApiFactory.Password });
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Contains("deactivated", await blocked.ProblemDetailAsync());
        Assert.Equal("Inactive", (await (await _admin.GetAsync($"/api/users/{id}")).JsonAsync()).GetProperty("status").GetString());

        (await _admin.PostAsync($"/api/users/{id}/activate", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await _api.NewClient().PostAsJsonAsync("/api/auth/login", new { email = "ana@feria.test", password = ApiFactory.Password })).StatusCode);
    }

    [Fact]
    public async Task The_super_admin_cannot_deactivate_their_own_account()
    {
        var adminId = await _admin.MyIdAsync();

        var response = await _admin.PostAsync($"/api/users/{adminId}/deactivate", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("own account", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task An_already_deactivated_user_cannot_be_deactivated_again_and_an_active_one_cannot_be_activated()
    {
        var id = await _api.CreateUserAsync("ana@feria.test", "Ana");
        Assert.Equal(HttpStatusCode.BadRequest, (await _admin.PostAsync($"/api/users/{id}/activate", null)).StatusCode);

        (await _admin.PostAsync($"/api/users/{id}/deactivate", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.BadRequest, (await _admin.PostAsync($"/api/users/{id}/deactivate", null)).StatusCode);
    }

    [Fact]
    public async Task A_user_that_never_set_a_password_goes_back_to_pending_when_reactivated()
    {
        var id = await CreateAsync("nuevo@feria.test");
        (await _admin.PostAsync($"/api/users/{id}/deactivate", null)).EnsureSuccessStatusCode();

        (await _admin.PostAsync($"/api/users/{id}/activate", null)).EnsureSuccessStatusCode();

        Assert.Equal("Pending", (await (await _admin.GetAsync($"/api/users/{id}")).JsonAsync()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Deactivating_a_user_closes_the_sessions_they_already_had_open()
    {
        var id = await _api.CreateUserAsync("ana@feria.test", "Ana");
        var ana = await _api.LoginAsync("ana@feria.test");
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync("/api/auth/me")).StatusCode);

        (await _admin.PostAsync($"/api/users/{id}/deactivate", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await ana.GetAsync("/api/auth/me")).StatusCode);
    }

    // ---- List ----

    private async Task CreateFiveUsersAsync()
    {
        foreach (var n in new[] { "Carla", "Ana", "Eva", "Beto", "Dani" }) await _api.CreateUserAsync($"{n.ToLower()}@feria.test", n);
    }

    [Fact]
    public async Task The_list_is_paginated_sorted_by_name_and_reports_the_total()
    {
        await CreateFiveUsersAsync(); // plus the super admin: 6 in total

        var first = await (await _admin.GetAsync("/api/users?page=1&pageSize=4")).JsonAsync();
        var second = await (await _admin.GetAsync("/api/users?page=2&pageSize=4")).JsonAsync();

        Assert.Equal(6, first.GetProperty("total").GetInt32());
        Assert.Equal(1, first.GetProperty("page").GetInt32());
        Assert.Equal(4, first.GetProperty("pageSize").GetInt32());
        var names = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray())
            .Select(i => i.GetProperty("fullName").GetString()).ToList();
        Assert.Equal(["Administración", "Ana", "Beto", "Carla", "Dani", "Eva"], names);
        Assert.Equal(2, second.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task A_page_past_the_end_is_empty()
    {
        await CreateFiveUsersAsync();

        var result = await (await _admin.GetAsync("/api/users?page=9&pageSize=4")).JsonAsync();

        Assert.Equal(0, result.GetProperty("items").GetArrayLength());
        Assert.Equal(6, result.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("status=Desconocido")]
    [InlineData("role=Desconocido")]
    public async Task Invalid_paging_or_filters_are_rejected(string query)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _admin.GetAsync("/api/users?" + query)).StatusCode);
    }

    [Fact]
    public async Task The_list_filters_by_status()
    {
        var id = await _api.CreateUserAsync("ana@feria.test", "Ana");
        await CreateAsync("nuevo@feria.test", "Nuevo");
        (await _admin.PostAsync($"/api/users/{id}/deactivate", null)).EnsureSuccessStatusCode();

        var inactive = await (await _admin.GetAsync("/api/users?status=Inactive")).JsonAsync();
        var pending = await (await _admin.GetAsync("/api/users?status=Pending")).JsonAsync();

        Assert.Equal("Ana", Assert.Single(inactive.GetProperty("items").EnumerateArray()).GetProperty("fullName").GetString());
        Assert.Equal("Nuevo", Assert.Single(pending.GetProperty("items").EnumerateArray()).GetProperty("fullName").GetString());
    }

    // ---- Roles ----

    [Fact]
    public async Task The_super_admin_gives_an_event_role_that_shows_in_the_session_menu_and_in_the_role_filter()
    {
        var teacher = await _api.CreateUserAsync("profe@feria.test", "Profe");
        await _api.CreateUserAsync("otra@feria.test", "Otra");
        var eventId = await _api.CreateEventAsync(await _admin.MyIdAsync(), "Feria 2026", closed: false);

        var assign = await _admin.PostAsJsonAsync($"/api/events/{eventId}/roles", new { userId = teacher, role = "Teacher" });

        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);
        var profe = await _api.LoginAsync("profe@feria.test");
        var role = Assert.Single((await (await profe.GetAsync("/api/auth/me")).JsonAsync()).GetProperty("eventRoles").EnumerateArray());
        Assert.Equal("Teacher", role.GetProperty("role").GetString());
        Assert.Equal(eventId, role.GetProperty("eventId").GetGuid());
        var teachers = await (await _admin.GetAsync("/api/users?role=Teacher")).JsonAsync();
        Assert.Equal("Profe", Assert.Single(teachers.GetProperty("items").EnumerateArray()).GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task Only_the_super_admin_assigns_roles()
    {
        var ana = await _api.CreateUserAsync("ana@feria.test", "Ana");
        var eventId = await _api.CreateEventAsync(await _admin.MyIdAsync(), "Feria 2026", closed: false);
        var client = await _api.LoginAsync("ana@feria.test");

        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/roles", new { userId = ana, role = "Teacher" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_super_admin_role_filter_returns_only_the_super_admin()
    {
        await CreateFiveUsersAsync();

        var result = await (await _admin.GetAsync("/api/users?role=SuperAdmin")).JsonAsync();

        Assert.Equal(ApiFactory.AdminEmail, Assert.Single(result.GetProperty("items").EnumerateArray()).GetProperty("email").GetString());
    }
}
