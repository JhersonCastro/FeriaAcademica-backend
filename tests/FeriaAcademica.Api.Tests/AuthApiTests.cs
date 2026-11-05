using System.Net;
using System.Net.Http.Json;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-001: sign in, sign out, the session, and the temporary lockout after 5 failed attempts.</summary>
public class AuthApiTests : IAsyncLifetime
{
    private const string Email = "ana@feria.test";

    private readonly ApiFactory _api = new();

    public async Task InitializeAsync() => await _api.CreateUserAsync(Email, "Ana");

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    [Fact]
    public async Task Valid_credentials_start_a_session_that_identifies_the_user()
    {
        var client = _api.NewClient();

        var response = await Login(client, Email, ApiFactory.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await (await client.GetAsync("/api/auth/me")).JsonAsync();
        Assert.Equal(Email, me.GetProperty("email").GetString());
        Assert.False(me.GetProperty("isSuperAdmin").GetBoolean());
    }

    [Fact]
    public async Task The_super_admin_is_marked_as_such_in_the_session()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);

        var me = await (await admin.GetAsync("/api/auth/me")).JsonAsync();

        Assert.True(me.GetProperty("isSuperAdmin").GetBoolean());
    }

    [Fact]
    public async Task Without_a_session_the_protected_endpoints_answer_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.NewClient().GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Logging_out_closes_the_session()
    {
        var client = await _api.LoginAsync(Email);

        (await client.PostAsync("/api/auth/logout", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task A_wrong_password_and_an_unknown_email_get_the_same_error()
    {
        var wrongPassword = await Login(_api.NewClient(), Email, "incorrecta");
        var unknownEmail = await Login(_api.NewClient(), "nadie@feria.test", ApiFactory.Password);

        Assert.Equal(HttpStatusCode.BadRequest, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownEmail.StatusCode);
        Assert.Equal(await unknownEmail.ProblemDetailAsync(), await wrongPassword.ProblemDetailAsync());
    }

    [Fact]
    public async Task Email_and_password_are_required()
    {
        var response = await Login(_api.NewClient(), "", "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Five_consecutive_failed_attempts_lock_the_account_even_for_the_right_password()
    {
        var client = _api.NewClient();
        for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.BadRequest, (await Login(client, Email, "incorrecta")).StatusCode);

        var withRightPassword = await Login(client, Email, ApiFactory.Password);

        Assert.Equal(HttpStatusCode.BadRequest, withRightPassword.StatusCode);
        Assert.Contains("temporarily locked", await withRightPassword.ProblemDetailAsync());
    }

    [Fact]
    public async Task Four_failed_attempts_do_not_lock_the_account()
    {
        var client = _api.NewClient();
        for (var i = 0; i < 4; i++) await Login(client, Email, "incorrecta");

        Assert.Equal(HttpStatusCode.OK, (await Login(client, Email, ApiFactory.Password)).StatusCode);
    }

    [Fact]
    public async Task A_successful_sign_in_resets_the_count_of_failed_attempts()
    {
        var client = _api.NewClient();
        for (var i = 0; i < 4; i++) await Login(client, Email, "incorrecta");
        await Login(client, Email, ApiFactory.Password);
        for (var i = 0; i < 4; i++) await Login(client, Email, "incorrecta");

        Assert.Equal(HttpStatusCode.OK, (await Login(client, Email, ApiFactory.Password)).StatusCode);
    }

    [Fact]
    public async Task The_lockout_only_affects_the_account_that_failed()
    {
        await _api.CreateUserAsync("beto@feria.test", "Beto");
        for (var i = 0; i < 5; i++) await Login(_api.NewClient(), Email, "incorrecta");

        Assert.Equal(HttpStatusCode.OK, (await Login(_api.NewClient(), "beto@feria.test", ApiFactory.Password)).StatusCode);
    }

    [Fact]
    public async Task The_lockout_is_temporary_and_lasts_15_minutes()
    {
        for (var i = 0; i < 5; i++) await Login(_api.NewClient(), Email, "incorrecta");

        var until = await _api.Arrange(async (_, users) => (await users.FindByEmailAsync(Email))!.LockoutEnd);

        Assert.NotNull(until);
        var remaining = until!.Value - DateTimeOffset.UtcNow;
        Assert.InRange(remaining, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
    }
}
