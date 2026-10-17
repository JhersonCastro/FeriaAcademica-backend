using FeriaAcademica.Application.Features.Events.Queries.ListEvents;
using FeriaAcademica.Application.Features.Works.Queries.GetWork;
using FeriaAcademica.Application.Features.Works.Queries.ListMyWorks;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
using Microsoft.EntityFrameworkCore;

namespace FeriaAcademica.Infrastructure.Persistence;

internal sealed class AcademicEventRepository(AppDbContext db) : IAcademicEventRepository
{
    public Task<AcademicEvent?> GetAsync(Guid id, CancellationToken ct) => db.Events.Include(e => e.Stages).FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<AcademicEvent?> GetByParticipationAsync(Guid participationId, CancellationToken ct) =>
        db.Events.Include(e => e.Stages)
            .FirstOrDefaultAsync(e => db.Participations.Any(p => p.Id == participationId && p.EventId == e.Id), ct);

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

    public async Task AddAsync(Work work, CancellationToken ct) => await db.Works.AddAsync(work, ct);
}

internal sealed class WorkReadModel(AppDbContext db) : IWorkReadModel
{
    public async Task<WorkDto?> GetAsync(Guid id, Guid authorId, CancellationToken ct)
    {
        var w = await (
                from x in db.Works.AsNoTracking()
                join p in db.Participations on x.ParticipationId equals p.Id
                join e in db.Events on p.EventId equals e.Id
                where x.Id == id && p.UserId == authorId
                select new
                {
                    x.Id, x.ParticipationId, EventId = e.Id, EventName = e.Name, x.ProfessorId, x.Title, x.Summary, x.Status, x.CurrentAttempt,
                    x.RejectionCount, x.MaxRejections, x.SubmittedAt,
                    SubmissionDeadline = db.EventStages
                        .Where(s => s.EventId == e.Id && s.Type == StageType.Reception && s.Status != StageStatus.Disabled)
                        .Select(s => s.EndDate).FirstOrDefault(),
                    x.Closure, x.ClosureReason, x.ClosedAt,
                    Artifacts = x.Artifacts.OrderBy(a => a.Type).ThenByDescending(a => a.Version)
                        .Select(a => new { a.Id, a.Type, a.Version, a.FileUrl, a.LinkUrl, a.UploadedAt }).ToList(),
                    Reviews = x.ReviewQueue.Where(q => q.Result != null).OrderBy(q => q.Attempt)
                        .Select(q => new { q.Attempt, q.Result, q.Observations, q.ResolvedAt }).ToList(),
                })
            .FirstOrDefaultAsync(ct);

        return w is null ? null : new WorkDto(w.Id, w.ParticipationId, w.EventId, w.EventName, w.ProfessorId, w.Title, w.Summary, w.Status.ToString(),
            w.CurrentAttempt, w.RejectionCount, w.MaxRejections, w.SubmittedAt, w.SubmissionDeadline, w.Closure?.ToString(), w.ClosureReason, w.ClosedAt,
            w.Artifacts.Select(a => new WorkArtifactDto(a.Id, a.Type.ToString(), a.Version, a.FileUrl, a.LinkUrl, a.UploadedAt)).ToList(),
            w.Reviews.Select(r => new WorkReviewDto(r.Attempt, r.Result!.Value.ToString(), r.Observations, r.ResolvedAt)).ToList());
    }
}

internal sealed class MyWorkReadModel(AppDbContext db) : IMyWorkReadModel
{
    public async Task<IReadOnlyList<MyWorkDto>> ListAsync(Guid authorId, CancellationToken ct)
    {
        var rows = await (
                from w in db.Works.AsNoTracking()
                join p in db.Participations on w.ParticipationId equals p.Id
                join e in db.Events on p.EventId equals e.Id
                where p.UserId == authorId
                select new
                {
                    w.Id, EventId = e.Id, EventName = e.Name, w.Title, w.Status, w.CurrentAttempt, w.RejectionCount, w.MaxRejections, w.SubmittedAt,
                    SubmissionDeadline = db.EventStages
                        .Where(s => s.EventId == e.Id && s.Type == StageType.Reception && s.Status != StageStatus.Disabled)
                        .Select(s => s.EndDate).FirstOrDefault(),
                    LastReview = w.ReviewQueue.Where(q => q.Result != null).OrderByDescending(q => q.Attempt)
                        .Select(q => new { q.Result, q.Observations }).FirstOrDefault(),
                })
            .ToListAsync(ct);

        return rows.OrderBy(r => r.EventName).ThenBy(r => r.Title)
            .Select(r => new MyWorkDto(r.Id, r.EventId, r.EventName, r.Title, r.Status.ToString(), r.CurrentAttempt, r.RejectionCount, r.MaxRejections,
                r.SubmittedAt, r.SubmissionDeadline, r.LastReview?.Result?.ToString(), r.LastReview?.Observations))
            .ToList();
    }
}

internal sealed class ParticipationRepository(AppDbContext db) : IParticipationRepository
{
    public Task<Participation?> GetAsync(Guid id, CancellationToken ct) => db.Participations.FirstOrDefaultAsync(p => p.Id == id, ct);
}
