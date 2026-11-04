using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Institutions;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Users;
using FeriaAcademica.Domain.Works;
using FeriaAcademica.Infrastructure.Identity;
using FeriaAcademica.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FeriaAcademica.Api.Tests;

/// <summary>
/// The real API (controllers, Mediator, Identity cookie, EF Core) on an in-memory SQLite database, so the tests need no PostgreSQL.
/// Each factory is its own empty database; tests arrange their data through <see cref="Arrange"/>.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@feria.test";
    public const string Password = "Passw0rd!2026";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.UseSetting("SuperAdmin:Email", AdminEmail);
        builder.UseSetting("SuperAdmin:Password", Password);
        builder.UseSetting("SuperAdmin:FullName", "Administración");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            // The API seeds the super admin as soon as it starts, so the schema has to exist before that.
            using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
            db.Database.EnsureCreated();
        });
    }

    /// <summary>A client that keeps the session cookie (the API only sends it over HTTPS outside Development).</summary>
    public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });

    public async Task<HttpClient> LoginAsync(string email, string password = Password)
    {
        var client = NewClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Runs setup code against the database, outside of HTTP.</summary>
    public async Task<T> Arrange<T>(Func<AppDbContext, UserManager<ApplicationUser>, Task<T>> setup)
    {
        using var scope = Services.CreateScope();
        return await setup(scope.ServiceProvider.GetRequiredService<AppDbContext>(), scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
    }

    public Task<Guid> CreateUserAsync(string email, string fullName = "Persona de prueba") => Arrange(async (_, users) =>
    {
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = fullName, Status = UserStatus.Active };
        var result = await users.CreateAsync(user, Password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        return user.Id;
    });

    /// <summary>An event in the given state, with its institution.</summary>
    public Task<Guid> CreateEventAsync(Guid createdBy, string name, bool closed) => Arrange(async (db, _) =>
    {
        var institution = Institution.Create("Universidad " + Guid.NewGuid().ToString("N"));
        var academicEvent = AcademicEvent.Create(institution.Id, name, createdBy);
        academicEvent.AddStage(StageType.Registration, active: true);
        academicEvent.Publish();
        if (closed) academicEvent.Close();
        db.AddRange(institution, academicEvent);
        await db.SaveChangesAsync();
        return academicEvent.Id;
    });

    /// <summary>A work of the participant that the professor has already approved (admitted); it is not yet final.</summary>
    public Task<Guid> CreateApprovedWorkAsync(Guid eventId, Guid participantId, Guid professorId, string title = "Seguidor solar") => Arrange(async (db, _) =>
    {
        var participation = Participation.Create(eventId, participantId);
        var now = DateTime.UtcNow;
        var work = Work.Create(participation.Id, professorId, title, null, 3, participantId, now.AddMinutes(-10));
        work.AddArtifact(ArtifactType.Document, "https://files.test/draft.pdf", null, now.AddMinutes(-9));
        work.Submit(participantId, now.AddMinutes(-8));
        work.StartReview(professorId, now.AddMinutes(-7));
        work.Admit(professorId, now.AddMinutes(-6));
        db.AddRange(participation, work);
        await db.SaveChangesAsync();
        return work.Id;
    });

    public Task AddFinalVersionAsync(Guid workId) => Arrange(async (db, _) =>
    {
        var work = await db.Works.Include(w => w.Artifacts).SingleAsync(w => w.Id == workId);
        work.SubmitFinalVersion("https://files.test/final.pdf", "final.pdf", 1_000_000, DateTime.UtcNow);
        await db.SaveChangesAsync();
        return 0;
    });

    public Task<Guid> CreateDraftWorkAsync(Guid eventId, Guid participantId, Guid professorId) => Arrange(async (db, _) =>
    {
        var participation = Participation.Create(eventId, participantId);
        var work = Work.Create(participation.Id, professorId, "Borrador", null, 3, participantId, DateTime.UtcNow);
        db.AddRange(participation, work);
        await db.SaveChangesAsync();
        return work.Id;
    });

    /// <summary>The person registered for the event; with <paramref name="attended"/> the door scan is recorded too.</summary>
    public Task RegisterAsync(Guid eventId, Guid userId, bool attended, Guid? scannedBy = null) => Arrange(async (db, _) =>
    {
        var registration = Registration.Create(eventId, userId, Guid.NewGuid().ToString("N"));
        db.Add(registration);
        if (attended) db.Add(Attendance.Create(registration.Id, scannedBy ?? userId, DateTime.UtcNow));
        await db.SaveChangesAsync();
        return 0;
    });

    public override ValueTask DisposeAsync()
    {
        _connection.Dispose();
        return base.DisposeAsync();
    }
}
