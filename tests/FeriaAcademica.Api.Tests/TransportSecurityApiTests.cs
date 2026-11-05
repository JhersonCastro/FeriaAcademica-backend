using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FeriaAcademica.Api.Tests;

/// <summary>HU-080: passwords are stored with a secure hash, the API only talks over HTTPS and the session expires.</summary>
public class TransportSecurityApiTests : IAsyncLifetime
{
    private const string Email = "ana@feria.test";

    private readonly ApiFactory _api = new();

    public async Task InitializeAsync() => await _api.CreateUserAsync(Email);

    public async Task DisposeAsync() => await _api.DisposeAsync();

    // ---- passwords ----

    [Fact]
    public async Task The_password_is_stored_as_a_salted_PBKDF2_hash_and_never_as_written()
    {
        var hash = await _api.Arrange(async (_, users) => (await users.FindByEmailAsync(Email))!.PasswordHash!);
        var other = await _api.Arrange(async (_, users) =>
        {
            var id = await _api.CreateUserAsync("otra@feria.test");
            return (await users.FindByIdAsync(id.ToString()))!.PasswordHash!;
        });

        Assert.DoesNotContain(ApiFactory.Password, hash);
        Assert.NotEqual(other, hash); // same password, different salt

        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(0x01, bytes[0]); // Identity's V3 format: PBKDF2 with a random salt
        var iterations = (bytes[5] << 24) | (bytes[6] << 16) | (bytes[7] << 8) | bytes[8];
        Assert.True(iterations >= 100_000, $"{iterations} PBKDF2 iterations");
    }

    // ---- HTTPS ----

    [Fact]
    public async Task A_request_over_plain_HTTP_is_redirected_to_HTTPS()
    {
        var response = await _api.NewRawClient("http://feria.example").GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal("https://feria.example/api/events", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Credentials_sent_over_plain_HTTP_do_not_start_a_session()
    {
        var response = await _api.NewRawClient("http://feria.example").PostAsJsonAsync("/api/auth/login", new { email = Email, password = ApiFactory.Password });

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Browsers_are_told_to_keep_using_HTTPS()
    {
        var response = await _api.NewRawClient("https://feria.example").GetAsync("/api/events");

        Assert.Contains("max-age=", Assert.Single(response.Headers.GetValues("Strict-Transport-Security")));
    }

    // ---- session ----

    [Fact]
    public async Task The_session_cookie_cannot_be_read_by_scripts_or_travel_over_plain_HTTP()
    {
        var response = await _api.NewRawClient("https://feria.example").PostAsJsonAsync("/api/auth/login", new { email = Email, password = ApiFactory.Password });

        response.EnsureSuccessStatusCode();
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("feria.auth="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_session_expires_after_8_hours_without_use()
    {
        var options = _api.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);

        Assert.Equal(TimeSpan.FromHours(8), options.ExpireTimeSpan);
        Assert.True(options.SlidingExpiration);
    }

    // ---- who may call the API from a browser ----

    private async Task<HttpResponseMessage> Preflight(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/events");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return await _api.NewClient().SendAsync(request);
    }

    [Fact]
    public async Task The_configured_frontend_may_call_the_API_with_the_session_cookie()
    {
        var response = await Preflight("http://localhost:4200");

        Assert.Equal("http://localhost:4200", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
    }

    [Fact]
    public async Task Any_other_site_may_not()
    {
        var response = await Preflight("https://sitio-ajeno.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task The_allowed_origin_follows_the_configured_frontend_address()
    {
        using var api = new ConfiguredFrontendFactory();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/events");
        request.Headers.Add("Origin", "https://feria.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await api.NewClient().SendAsync(request);

        Assert.Equal("https://feria.example", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    private sealed class ConfiguredFrontendFactory : ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Frontend:BaseUrl", "https://feria.example/");
        }
    }
}
