using FeriaAcademica.Application;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Infrastructure;
using FeriaAcademica.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(config);

// Cookie-based auth through ASP.NET Core Identity. Google only identifies the user;
// Identity then creates/links the local account and issues its own cookie.
var auth = builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme);
auth.AddIdentityCookies(c => c.ApplicationCookie?.Configure(o =>
{
    o.Cookie.Name = "feria.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;        // localhost:4200 and the API on another localhost port are same-site
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
    // API: answer 401/403 instead of redirecting to a login page.
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
}));

if (!string.IsNullOrWhiteSpace(config["Authentication:Google:ClientId"]))
{
    auth.AddGoogle(o =>
    {
        o.ClientId = config["Authentication:Google:ClientId"]!;
        o.ClientSecret = config["Authentication:Google:ClientSecret"]!;
        o.SignInScheme = IdentityConstants.ExternalScheme;
    });
    // Microsoft and other providers can be added the same way (AddMicrosoftAccount).
}

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// Domain rule violations -> 400 ProblemDetails.
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (DomainException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Business rule violation", Detail = ex.Message, Status = 400 });
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated(); // no migrations yet
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
