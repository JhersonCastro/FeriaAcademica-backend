using FeriaAcademica.Application.Features.Events.Queries.ListEvents;
using FeriaAcademica.Application.Features.Invitations.Queries.GetInvitation;
using FeriaAcademica.Application.Features.Users.Queries.GetUser;
using FeriaAcademica.Application.Features.Users.Queries.ListUsers;
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

internal sealed class RoleAssignmentRepository(AppDbContext db) : IRoleAssignmentRepository
{
    public async Task<IReadOnlyList<EventRole>> GetRolesAsync(Guid userId, Guid eventId, CancellationToken ct) =>
        await db.RoleAssignments.Where(r => r.UserId == userId && r.EventId == eventId).Select(r => r.Role).ToListAsync(ct);

    public async Task AddAsync(RoleAssignment assignment, CancellationToken ct) => await db.RoleAssignments.AddAsync(assignment, ct);
}

internal sealed class InvitationRepository(AppDbContext db) : IInvitationRepository
{
    public Task<Invitation?> GetByTokenAsync(string token, CancellationToken ct) => db.Invitations.FirstOrDefaultAsync(i => i.Token == token, ct);
    public async Task AddAsync(Invitation invitation, CancellationToken ct) => await db.Invitations.AddAsync(invitation, ct);
}

internal sealed class InvitationReadModel(AppDbContext db) : IInvitationReadModel
{
    public async Task<InvitationDto?> GetByTokenAsync(string token, DateTime now, CancellationToken ct)
    {
        var i = await db.Invitations.AsNoTracking().Where(x => x.Token == token)
            .Join(db.Events, x => x.EventId, e => e.Id, (x, e) => new
            {
                x.EventId, EventName = e.Name, x.Email, x.Role, x.Status, x.ExpiresAt,
                HasAccount = db.Users.Any(u => u.NormalizedEmail == x.Email.ToUpper()),
            })
            .FirstOrDefaultAsync(ct);
        if (i is null) return null;

        var status = i.Status == InvitationStatus.Pending && now >= i.ExpiresAt ? InvitationStatus.Expired : i.Status;
        return new InvitationDto(i.EventId, i.EventName, i.Email, i.Role.ToString(), status.ToString(), i.ExpiresAt, i.HasAccount);
    }
}

internal sealed class UserReadModel(AppDbContext db) : IUserReadModel, IUserListReadModel
{
    public async Task<UserDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (u is null) return null;

        var roles = await db.RoleAssignments.AsNoTracking().Where(r => r.UserId == id)
            .Join(db.Events, r => r.EventId, e => e.Id, (r, e) => new { r.EventId, e.Name, r.Role })
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return new UserDto(u.Id, u.Email!, u.FullName, u.PhoneNumber, u.BirthDate, u.GuardianName, u.GuardianEmail, u.IsSuperAdmin,
            u.Status.ToString(), u.CreatedAt, roles.Select(r => new EventRoleDto(r.EventId, r.Name, r.Role.ToString())).ToList());
    }

    public async Task<PagedResult<UserSummaryDto>> ListAsync(UserFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();
        if (filter.Search is { } search)
        {
            var pattern = "%" + search.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            query = query.Where(u => EF.Functions.ILike(u.FullName, pattern) || EF.Functions.ILike(u.Email!, pattern));
        }
        if (filter.SuperAdminsOnly) query = query.Where(u => u.IsSuperAdmin);
        if (filter.Role is { } role) query = query.Where(u => db.RoleAssignments.Any(r => r.UserId == u.Id && r.Role == role));
        if (filter.Status is { } status) query = query.Where(u => u.Status == status);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(u => u.FullName).ThenBy(u => u.Email)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new
            {
                u.Id, u.Email, u.FullName, u.IsSuperAdmin, u.Status,
                Roles = db.RoleAssignments.Where(r => r.UserId == u.Id).Select(r => r.Role).Distinct().ToList(),
            })
            .ToListAsync(ct);

        var items = rows.Select(u => new UserSummaryDto(u.Id, u.Email!, u.FullName, u.IsSuperAdmin, u.Status.ToString(),
            u.Roles.Select(r => r.ToString()).Order().ToList())).ToList();
        return new PagedResult<UserSummaryDto>(items, page, pageSize, total);
    }
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
