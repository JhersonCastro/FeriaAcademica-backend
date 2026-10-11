using FeriaAcademica.Application.Features.Events.Queries.ListEvents;
using FeriaAcademica.Application.Features.Works.Queries.GetWork;
using FeriaAcademica.Domain.Events;
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
