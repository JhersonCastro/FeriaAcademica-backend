using FeriaAcademica.Application.Features.Events.Queries.ListEvents;
using FeriaAcademica.Application.Features.Notifications.Queries.ListMyNotifications;
using FeriaAcademica.Application.Features.Works.Queries.GetWork;
using FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Notifications;
using FeriaAcademica.Domain.Works;
using Microsoft.EntityFrameworkCore;

namespace FeriaAcademica.Infrastructure.Persistence;

internal sealed class AcademicEventRepository(AppDbContext db) : IAcademicEventRepository
{
    public Task<AcademicEvent?> GetAsync(Guid id, CancellationToken ct) => db.Events.Include(e => e.Stages).FirstOrDefaultAsync(e => e.Id == id, ct);
    public async Task AddAsync(AcademicEvent academicEvent, CancellationToken ct) => await db.Events.AddAsync(academicEvent, ct);
}

internal sealed class EventReadModel(AppDbContext db) : IEventReadModel
{
    public async Task<IReadOnlyList<EventDto>> ListAsync(CancellationToken ct) =>
        await db.Events.AsNoTracking().OrderBy(e => e.StartDate).ThenBy(e => e.Name)
            .Select(e => new EventDto(e.Id, e.InstitutionId, e.Name, e.Status.ToString(), e.StartDate, e.EndDate))
            .ToListAsync(ct);
}

internal sealed class WorkRepository(AppDbContext db) : IWorkRepository
{
    public Task<Work?> GetAsync(Guid id, CancellationToken ct) =>
        db.Works.Include(w => w.Artifacts).Include(w => w.ReviewQueue).FirstOrDefaultAsync(w => w.Id == id, ct);
}

internal sealed class WorkReadModel(AppDbContext db) : IWorkReadModel
{
    public async Task<WorkDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var w = await db.Works.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, x.ParticipationId, x.ProfessorId, x.Title, x.Summary, x.Status, x.CurrentAttempt, x.RejectionCount,
                x.MaxRejections, x.SubmittedAt, x.Closure, x.ClosureReason, x.ClosedAt, ArtifactCount = x.Artifacts.Count,
            })
            .FirstOrDefaultAsync(ct);

        return w is null ? null : new WorkDto(w.Id, w.ParticipationId, w.ProfessorId, w.Title, w.Summary, w.Status.ToString(),
            w.CurrentAttempt, w.RejectionCount, w.MaxRejections, w.SubmittedAt, w.Closure?.ToString(), w.ClosureReason, w.ClosedAt, w.ArtifactCount);
    }
}

internal sealed class AssignedWorkReadModel(AppDbContext db) : IAssignedWorkReadModel
{
    public async Task<IReadOnlyList<AssignedWorkDto>> ListAsync(Guid reviewerId, CancellationToken ct)
    {
        var rows = await (
                from w in db.Works.AsNoTracking()
                join p in db.Participations on w.ParticipationId equals p.Id
                join e in db.Events on p.EventId equals e.Id
                where w.ProfessorId == reviewerId && (w.Status == WorkStatus.Submitted || w.Status == WorkStatus.InReview)
                select new
                {
                    w.Id, EventId = e.Id, EventName = e.Name, w.Title, w.Summary, w.Status, w.CurrentAttempt, w.RejectionCount, w.MaxRejections,
                    EnqueuedAt = w.ReviewQueue.Where(q => q.Attempt == w.CurrentAttempt).Select(q => (DateTime?)q.EnqueuedAt).FirstOrDefault(),
                    Artifacts = w.Artifacts.Select(a => new ArtifactDto(a.Id, a.Type.ToString(), a.Version, a.FileUrl, a.LinkUrl, a.UploadedAt)).ToList(),
                })
            .ToListAsync(ct);

        // Oldest in the queue first; only the latest version of each artifact type is shown.
        return rows.OrderBy(r => r.EnqueuedAt)
            .Select(r => new AssignedWorkDto(r.Id, r.EventId, r.EventName, r.Title, r.Summary, r.Status.ToString(),
                r.CurrentAttempt, r.RejectionCount, r.MaxRejections, r.EnqueuedAt,
                r.Artifacts.GroupBy(a => a.Type).Select(g => g.MaxBy(a => a.Version)!).OrderBy(a => a.Type).ToList()))
            .ToList();
    }
}

internal sealed class ParticipationRepository(AppDbContext db) : IParticipationRepository
{
    public Task<Participation?> GetAsync(Guid id, CancellationToken ct) => db.Participations.FirstOrDefaultAsync(p => p.Id == id, ct);
}

internal sealed class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public Task<Notification?> GetAsync(Guid id, CancellationToken ct) => db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);
    public async Task AddAsync(Notification notification, CancellationToken ct) => await db.Notifications.AddAsync(notification, ct);
}

internal sealed class NotificationReadModel(AppDbContext db) : INotificationReadModel
{
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(Guid userId, bool unreadOnly, CancellationToken ct) =>
        await db.Notifications.AsNoTracking().Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead))
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.EventId, n.Type, n.Message, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
}
