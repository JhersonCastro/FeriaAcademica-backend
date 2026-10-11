using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Evaluation;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Institutions;
using FeriaAcademica.Domain.Notifications;
using FeriaAcademica.Domain.Privacy;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Works;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FeriaAcademica.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<InstitutionMembership> InstitutionMemberships => Set<InstitutionMembership>();
    public DbSet<EventTemplate> EventTemplates => Set<EventTemplate>();
    public DbSet<AcademicEvent> Events => Set<AcademicEvent>();
    public DbSet<EventStage> EventStages => Set<EventStage>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Participation> Participations => Set<Participation>();
    public DbSet<Work> Works => Set<Work>();
    public DbSet<Artifact> Artifacts => Set<Artifact>();
    public DbSet<WorkHistoryEntry> WorkHistory => Set<WorkHistoryEntry>();
    public DbSet<ReviewQueueItem> ReviewQueue => Set<ReviewQueueItem>();
    public DbSet<JuryAssignment> JuryAssignments => Set<JuryAssignment>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<Criterion> Criteria => Set<Criterion>();
    public DbSet<Response> Responses => Set<Response>();
    public DbSet<WorkResult> WorkResults => Set<WorkResult>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<DataPolicy> DataPolicies => Set<DataPolicy>();
    public DbSet<Consent> Consents => Set<Consent>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        // Every state/kind enum is stored as text, as in the schema (text columns, not numbers).
        var enums = typeof(Entity).Assembly.GetTypes().Where(t => t.IsEnum).Append(typeof(UserStatus));
        foreach (var e in enums) c.Properties(e).HaveConversion(typeof(string)).HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // The schema declares no cascading deletes: nothing is removed implicitly.
        foreach (var fk in b.Model.GetEntityTypes().Where(t => t.ClrType.Namespace?.StartsWith("FeriaAcademica.Domain") == true).SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);
}
