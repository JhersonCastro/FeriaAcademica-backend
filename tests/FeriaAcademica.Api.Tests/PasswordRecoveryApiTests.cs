using System.Net;
using System.Net.Http.Json;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-003: recover the password from an emailed link that expires.</summary>
public class PasswordRecoveryApiTests : IAsyncLifetime
{
    private const string Email = "ana@feria.test";
    private const string NewPassword = "NuevaClave#2027";

    private readonly ApiFactory _api = new();
    private Guid _userId;

    public async Task InitializeAsync() => _userId = await _api.CreateUserAsync(Email, "Ana");

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private Task<HttpResponseMessage> Forgot(string email) => _api.NewClient().PostAsJsonAsync("/api/auth/forgot-password", new { email });

    private Task<HttpResponseMessage> Reset(Guid userId, string token, string password) =>
        _api.NewClient().PostAsJsonAsync("/api/auth/reset-password", new { userId, token, newPassword = password });

    private async Task<SentLink> RequestLinkAsync()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await Forgot(Email)).StatusCode);
        return Assert.Single(_api.Emails.Sent, e => e.Kind == "PasswordReset");
    }

    private async Task<HttpResponseMessage> TryLogin(string password) =>
        await _api.NewClient().PostAsJsonAsync("/api/auth/login", new { email = Email, password });

    [Fact]
    public async Task Asking_for_recovery_with_the_registered_email_sends_a_link_to_that_email()
    {
        var link = await RequestLinkAsync();

        Assert.Equal(Email, link.Email);
        Assert.Equal(_userId, link.UserId);
        Assert.False(string.IsNullOrWhiteSpace(link.Token));
    }

    [Fact]
    public async Task The_link_lets_the_user_define_a_new_password_and_sign_in_with_it()
    {
        var link = await RequestLinkAsync();

        var reset = await Reset(link.UserId, link.Token, NewPassword);

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await TryLogin(NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await TryLogin(ApiFactory.Password)).StatusCode);
    }

    [Fact]
    public async Task The_answer_is_the_same_whether_or_not_the_email_exists_and_nothing_is_sent_for_an_unknown_one()
    {
        var known = await Forgot(Email);
        var unknown = await Forgot("nadie@feria.test");

        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Single(_api.Emails.Sent);
    }

    [Fact]
    public async Task A_deactivated_account_gets_no_recovery_link()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        (await admin.PostAsync($"/api/users/{_userId}/deactivate", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NoContent, (await Forgot(Email)).StatusCode);

        Assert.Empty(_api.Emails.Sent);
    }

    [Fact]
    public async Task A_link_can_only_be_used_once()
    {
        var link = await RequestLinkAsync();
        (await Reset(link.UserId, link.Token, NewPassword)).EnsureSuccessStatusCode();

        var again = await Reset(link.UserId, link.Token, "OtraClave#2028");

        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Contains("invalid or has expired", await again.ProblemDetailAsync());
        Assert.Equal(HttpStatusCode.OK, (await TryLogin(NewPassword)).StatusCode);
    }

    [Fact]
    public async Task A_tampered_link_is_rejected()
    {
        var link = await RequestLinkAsync();

        var response = await Reset(link.UserId, link.Token[..^4] + "AAAA", NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid or has expired", await response.ProblemDetailAsync());
    }

    [Fact]
    public async Task A_link_does_not_work_for_another_account()
    {
        var other = await _api.CreateUserAsync("beto@feria.test", "Beto");
        var link = await RequestLinkAsync();

        var response = await Reset(other, link.Token, NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_new_password_must_meet_the_password_rules()
    {
        var link = await RequestLinkAsync();

        var response = await Reset(link.UserId, link.Token, "corta");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await TryLogin(ApiFactory.Password)).StatusCode);
    }

    [Fact]
    public async Task Recovering_the_password_ends_a_lockout_caused_by_failed_attempts()
    {
        for (var i = 0; i < 5; i++) await TryLogin("incorrecta");
        Assert.Equal(HttpStatusCode.BadRequest, (await TryLogin(ApiFactory.Password)).StatusCode); // locked, even with the right password
        var link = await RequestLinkAsync();

        (await Reset(link.UserId, link.Token, NewPassword)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.OK, (await TryLogin(NewPassword)).StatusCode);
    }

    [Fact]
    public async Task An_account_created_by_the_admin_defines_its_first_password_with_its_link_and_becomes_active()
    {
        var admin = await _api.LoginAsync(ApiFactory.AdminEmail);
        (await admin.PostAsJsonAsync("/api/users", new { email = "nuevo@feria.test", fullName = "Nuevo" })).EnsureSuccessStatusCode();
        var link = Assert.Single(_api.Emails.Sent, e => e.Kind == "SetPassword");

        (await Reset(link.UserId, link.Token, NewPassword)).EnsureSuccessStatusCode();

        var login = await _api.NewClient().PostAsJsonAsync("/api/auth/login", new { email = "nuevo@feria.test", password = NewPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("Active", (await login.JsonAsync()).GetProperty("status").GetString());
    }
}

/// <summary>The link expires after the configured time: here it is expired from the moment it is created.</summary>
public class ExpiredPasswordLinkApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new(tokenLifespan: TimeSpan.FromMinutes(-1));

    public async Task InitializeAsync() => await _api.CreateUserAsync("ana@feria.test", "Ana");

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task An_expired_link_cannot_define_a_password()
    {
        var client = _api.NewClient();
        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "ana@feria.test" });
        var link = Assert.Single(_api.Emails.Sent);

        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new { userId = link.UserId, token = link.Token, newPassword = "NuevaClave#2027" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid or has expired", await response.ProblemDetailAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new { email = "ana@feria.test", password = ApiFactory.Password })).StatusCode);
    }
}
