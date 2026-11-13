using FeriaAcademica.Application.Features.Attendance.Queries.GetAttendanceSummary;
using FeriaAcademica.Application.Features.Certificates.Queries.ListMyCertificates;
using FeriaAcademica.Application.Features.Events.Queries.GetEventReport;
using FeriaAcademica.Application.Features.Events.Queries.GetEventSummary;
using FeriaAcademica.Application.Features.Events.Queries.GetMyParticipation;
using FeriaAcademica.Application.Features.Events.Queries.ListEventProfessors;
using FeriaAcademica.Application.Features.Events.Queries.ListEvents;
using FeriaAcademica.Application.Features.Events.Queries.ListEventStages;
using FeriaAcademica.Application.Features.Invitations.Queries.GetInvitation;
using FeriaAcademica.Application.Features.Notifications.Queries.ListMyNotifications;
using FeriaAcademica.Application.Features.Registrations;
using FeriaAcademica.Application.Features.Registrations.Queries.GetMyRegistration;
using FeriaAcademica.Application.Features.Users.Queries.GetUser;
using FeriaAcademica.Application.Features.Users.Queries.ListUsers;
using FeriaAcademica.Application.Features.Works.Queries.DownloadWorkFile;
using FeriaAcademica.Application.Features.Works.Queries.GetWork;
using FeriaAcademica.Application.Features.Works.Queries.ListAssignedWorks;
using FeriaAcademica.Application.Features.Works.Queries.ListMyWorks;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Notifications;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Works;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            .Select(e => new
            {
                e.Id, e.InstitutionId, e.Name, e.Status, e.StartDate, e.EndDate, e.Capacity,
                Registered = db.Registrations.Count(r => r.EventId == e.Id && r.Status == RegistrationStatus.Registered),
                SubmissionDeadline = db.EventStages
                    .Where(s => s.EventId == e.Id && s.Type == StageType.Reception && s.Status != StageStatus.Disabled)
                    .Select(s => s.EndDate).FirstOrDefault(),
            })
            .Select(e => new EventDto(e.Id, e.InstitutionId, e.Name, e.Status.ToString(), e.StartDate, e.EndDate,
                e.Capacity, e.Registered, e.Capacity == null ? null : Math.Max(0, e.Capacity.Value - e.Registered), e.SubmissionDeadline))
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
    public Task<Invitation?> GetAsync(Guid id, CancellationToken ct) => db.Invitations.FirstOrDefaultAsync(i => i.Id == id, ct);
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
            .Select(u => new { u.Id, u.Email, u.FullName, u.IsSuperAdmin, u.Status })
            .ToListAsync(ct);

        // Roles of the users on this page only, in one query (no correlated subquery per row).
        var ids = rows.Select(u => u.Id).ToList();
        var roles = (await db.RoleAssignments.AsNoTracking().Where(r => ids.Contains(r.UserId)).Select(r => new { r.UserId, r.Role }).Distinct().ToListAsync(ct))
            .ToLookup(r => r.UserId, r => r.Role.ToString());

        var items = rows.Select(u => new UserSummaryDto(u.Id, u.Email!, u.FullName, u.IsSuperAdmin, u.Status.ToString(), roles[u.Id].Order().ToList())).ToList();
        return new PagedResult<UserSummaryDto>(items, page, pageSize, total);
    }
}

internal sealed class RegistrationRepository(AppDbContext db) : IRegistrationRepository
{
    public Task<Registration?> GetAsync(Guid eventId, Guid userId, CancellationToken ct) =>
        db.Registrations.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId, ct);

    // The QR token has a unique index, so this is a single index lookup: the scan answers well under the 2 seconds the backlog asks for.
    public Task<Registration?> GetByQrTokenAsync(string qrToken, CancellationToken ct) =>
        db.Registrations.FirstOrDefaultAsync(r => r.QrToken == qrToken, ct);

    public Task<int> CountActiveAsync(Guid eventId, CancellationToken ct) =>
        db.Registrations.CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Registered, ct);

    public async Task AddAsync(Registration registration, CancellationToken ct) => await db.Registrations.AddAsync(registration, ct);
}

internal sealed class AttendanceRepository(AppDbContext db) : IAttendanceRepository
{
    public Task<Attendance?> GetByRegistrationAsync(Guid registrationId, CancellationToken ct) =>
        db.Attendances.AsNoTracking().FirstOrDefaultAsync(a => a.RegistrationId == registrationId, ct);

    public async Task<bool> TryAddAsync(Attendance attendance, CancellationToken ct)
    {
        await db.Attendances.AddAsync(attendance, ct);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The unique index on RegistrationId let another scan win the race.
            db.Entry(attendance).State = EntityState.Detached;
            return false;
        }
    }
}

internal sealed class RegistrationReadModel(AppDbContext db) : IRegistrationReadModel
{
    public async Task<RegistrationDto?> GetAsync(Guid eventId, Guid userId, CancellationToken ct)
    {
        var r = await db.Registrations.AsNoTracking().Where(x => x.EventId == eventId && x.UserId == userId)
            .Select(x => new
            {
                x.Id, x.EventId, x.QrToken, x.Status,
                User = db.Users.Where(u => u.Id == x.UserId).Select(u => new { u.FullName, u.Email }).First(),
                EnteredAt = db.Attendances.Where(a => a.RegistrationId == x.Id).Select(a => (DateTime?)a.At).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);
        return r is null ? null : new RegistrationDto(r.Id, r.EventId, r.QrToken, r.Status.ToString(), r.EnteredAt, r.User.FullName, r.User.Email!);
    }
}

internal sealed class AttendanceReadModel(AppDbContext db) : IAttendanceReadModel
{
    public async Task<AttendanceSummaryDto> GetSummaryAsync(Guid eventId, CancellationToken ct)
    {
        var active = db.Registrations.AsNoTracking().Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Registered);
        var registered = await active.CountAsync(ct);
        var entries = db.Attendances.AsNoTracking().Where(a => active.Any(r => r.Id == a.RegistrationId));
        var entered = await entries.CountAsync(ct);
        var last = await entries.MaxAsync(a => (DateTime?)a.At, ct);
        return new AttendanceSummaryDto(eventId, registered, entered, registered - entered, last);
    }
}

internal sealed class WorkRepository(AppDbContext db) : IWorkRepository
{
    public Task<Work?> GetAsync(Guid id, CancellationToken ct) =>
        db.Works.Include(w => w.Artifacts).Include(w => w.ReviewQueue).FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<Work?> GetWithFinalVersionAsync(Guid eventId, Guid userId, CancellationToken ct) =>
        db.Works.Include(w => w.Artifacts)
            .Where(w => w.Artifacts.Any(a => a.IsFinal) && db.Participations.Any(p => p.Id == w.ParticipationId && p.EventId == eventId && p.UserId == userId))
            .OrderBy(w => w.Title)
            .FirstOrDefaultAsync(ct);

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
                        .Select(a => new { a.Id, a.Type, a.Version, a.FileUrl, a.LinkUrl, a.UploadedAt, a.IsFinal }).ToList(),
                    Reviews = x.ReviewQueue.Where(q => q.Result != null).OrderBy(q => q.Attempt)
                        .Select(q => new { q.Attempt, q.Result, q.Observations, q.ResolvedAt }).ToList(),
                })
            .FirstOrDefaultAsync(ct);

        return w is null ? null : new WorkDto(w.Id, w.ParticipationId, w.EventId, w.EventName, w.ProfessorId, w.Title, w.Summary, w.Status.ToString(),
            w.CurrentAttempt, w.RejectionCount, w.MaxRejections, w.SubmittedAt, w.SubmissionDeadline, w.Closure?.ToString(), w.ClosureReason, w.ClosedAt,
            w.Artifacts.Select(a => new WorkArtifactDto(a.Id, a.Type.ToString(), a.Version, a.FileUrl, a.LinkUrl, a.UploadedAt, a.IsFinal)).ToList(),
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

internal sealed class WorkFileReadModel(AppDbContext db) : IWorkFileReadModel
{
    public async Task<WorkFileInfo?> FindAsync(Guid workId, string fileUrl, Guid userId, CancellationToken ct)
    {
        var row = await (
                from a in db.Artifacts.AsNoTracking()
                join w in db.Works on a.WorkId equals w.Id
                join p in db.Participations on w.ParticipationId equals p.Id
                where w.Id == workId && a.FileUrl == fileUrl
                select new
                {
                    a.Type, a.Version, p.EventId, AuthorId = p.UserId, ReviewerId = w.ProfessorId,
                    IsAssignedJury = db.JuryAssignments.Any(j => j.WorkId == w.Id && j.JuryId == userId),
                })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : new WorkFileInfo(row.Type.ToString(), row.Version, row.EventId, row.AuthorId, row.ReviewerId, row.IsAssignedJury);
    }
}

internal sealed class ParticipationRepository(AppDbContext db) : IParticipationRepository
{
    public Task<Participation?> GetAsync(Guid id, CancellationToken ct) => db.Participations.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Participation?> GetByEventAndUserAsync(Guid eventId, Guid userId, CancellationToken ct) =>
        db.Participations.FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId, ct);

    public async Task AddAsync(Participation participation, CancellationToken ct) => await db.Participations.AddAsync(participation, ct);
}

internal sealed class ParticipationReadModel(AppDbContext db) : IParticipationReadModel
{
    public Task<ParticipationDto?> GetAsync(Guid eventId, Guid userId, CancellationToken ct) =>
        (from p in db.Participations.AsNoTracking()
         where p.EventId == eventId && p.UserId == userId
         select new ParticipationDto(p.Id, p.EventId, db.Invitations.Where(i => i.Id == p.InvitationId).Select(i => i.SenderId).FirstOrDefault()))
        .FirstOrDefaultAsync(ct);
}

internal sealed class EventProfessorReadModel(AppDbContext db) : IEventProfessorReadModel
{
    public async Task<IReadOnlyList<ProfessorDto>> ListAsync(Guid eventId, CancellationToken ct) =>
        await (from r in db.RoleAssignments.AsNoTracking()
               join u in db.Users on r.UserId equals u.Id
               where r.EventId == eventId && r.Role == EventRole.Teacher
               orderby u.FullName
               select new ProfessorDto(u.Id, u.FullName))
            .ToListAsync(ct);
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

internal sealed class EventReportRepository(AppDbContext db) : IEventReportRepository
{
    public Task<EventReport?> GetByEventAsync(Guid eventId, CancellationToken ct) => db.EventReports.FirstOrDefaultAsync(r => r.EventId == eventId, ct);
    public async Task AddAsync(EventReport report, CancellationToken ct) => await db.EventReports.AddAsync(report, ct);
}

internal sealed class EventReportReadModel(AppDbContext db) : IEventReportReadModel
{
    public Task<EventReportDto?> GetAsync(Guid eventId, CancellationToken ct) =>
        db.EventReports.AsNoTracking().Where(r => r.EventId == eventId)
            .Join(db.Events, r => r.EventId, e => e.Id, (r, e) => new EventReportDto(r.EventId, e.Name, r.Html, r.UpdatedAt))
            .FirstOrDefaultAsync(ct);
}

internal sealed class EventStageReadModel(AppDbContext db) : IEventStageReadModel
{
    public async Task<IReadOnlyList<EventStageDto>?> ListAsync(Guid eventId, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId, ct)) return null;

        var stages = await db.EventStages.AsNoTracking().Where(s => s.EventId == eventId && s.Status != StageStatus.Disabled).ToListAsync(ct);
        // Sorted here: the type is stored as text, so the database would sort it alphabetically instead of in stage order.
        return stages.OrderBy(s => s.StartDate ?? DateTime.MaxValue).ThenBy(s => s.Type)
            .Select(s => new EventStageDto(s.Type.ToString(), s.Status.ToString(), s.StartDate, s.EndDate)).ToList();
    }
}

internal sealed class EventSummaryReadModel(AppDbContext db) : IEventSummaryReadModel
{
    public async Task<EventWorkCounts?> GetWorkCountsAsync(Guid eventId, CancellationToken ct)
    {
        var academicEvent = await db.Events.AsNoTracking().Where(e => e.Id == eventId).Select(e => new { e.Name, e.Status }).FirstOrDefaultAsync(ct);
        if (academicEvent is null) return null;

        var counts = await (
                from w in db.Works.AsNoTracking()
                join p in db.Participations on w.ParticipationId equals p.Id
                where p.EventId == eventId && w.SubmittedAt != null
                group w by w.Status into g
                select new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new EventWorkCounts(academicEvent.Name, academicEvent.Status.ToString(), counts.ToDictionary(c => c.Status, c => c.Count));
    }
}

internal sealed class CertificateRepository(AppDbContext db) : ICertificateRepository
{
    public Task<Certificate?> GetAsync(Guid eventId, Guid userId, CertificateType type, CancellationToken ct) =>
        db.Certificates.FirstOrDefaultAsync(c => c.EventId == eventId && c.UserId == userId && c.Type == type, ct);

    public async Task AddAsync(Certificate certificate, CancellationToken ct) => await db.Certificates.AddAsync(certificate, ct);
}

internal sealed class CertificateReadModel(AppDbContext db) : ICertificateReadModel
{
    public async Task<IReadOnlyList<MyCertificateDto>> ListAsync(Guid userId, CancellationToken ct)
    {
        var attended = await (
                from r in db.Registrations.AsNoTracking()
                join a in db.Attendances on r.Id equals a.RegistrationId
                join e in db.Events on r.EventId equals e.Id
                where r.UserId == userId
                select new { EventId = e.Id, EventName = e.Name, e.Status })
            .ToListAsync(ct);

        var participated = await (
                from w in db.Works.AsNoTracking()
                join p in db.Participations on w.ParticipationId equals p.Id
                join e in db.Events on p.EventId equals e.Id
                where p.UserId == userId && w.Artifacts.Any(a => a.IsFinal)
                select new { EventId = e.Id, EventName = e.Name, e.Status, WorkId = w.Id, WorkTitle = w.Title })
            .ToListAsync(ct);

        var roles = await (
                from r in db.RoleAssignments.AsNoTracking()
                join e in db.Events on r.EventId equals e.Id
                where r.UserId == userId
                select new { EventId = e.Id, EventName = e.Name, e.Status, r.Role })
            .ToListAsync(ct);
        var recognized = roles.Where(r => CertificateEligibility.RecognizedRoles.Contains(r.Role)).DistinctBy(r => r.EventId);

        var issued = await db.Certificates.AsNoTracking().Where(c => c.UserId == userId).ToListAsync(ct);
        string? CodeOf(Guid eventId, CertificateType type) => issued.FirstOrDefault(c => c.EventId == eventId && c.Type == type)?.Code;
        static bool Finished(EventStatus status) => status is EventStatus.Closed or EventStatus.Archived;

        return attended
            .Select(x => new MyCertificateDto(x.EventId, x.EventName, nameof(CertificateType.Attendance), null, null, Finished(x.Status),
                CodeOf(x.EventId, CertificateType.Attendance)))
            .Concat(participated.Select(x => new MyCertificateDto(x.EventId, x.EventName, nameof(CertificateType.Participation), x.WorkId, x.WorkTitle,
                Finished(x.Status), CodeOf(x.EventId, CertificateType.Participation))))
            .Concat(recognized.Select(x => new MyCertificateDto(x.EventId, x.EventName, nameof(CertificateType.Recognition), null, null, Finished(x.Status),
                CodeOf(x.EventId, CertificateType.Recognition))))
            .OrderBy(c => c.EventName).ThenBy(c => c.Type)
            .ToList();
    }
}
