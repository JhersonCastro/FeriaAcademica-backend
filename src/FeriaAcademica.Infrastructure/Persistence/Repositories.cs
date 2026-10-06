using FeriaAcademica.Application.Events.Queries;
using FeriaAcademica.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace FeriaAcademica.Infrastructure.Persistence;

internal sealed class AcademicEventRepository(AppDbContext db) : IAcademicEventRepository
{
    public Task<AcademicEvent?> GetAsync(Guid id, CancellationToken ct) => db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
    public async Task AddAsync(AcademicEvent academicEvent, CancellationToken ct) => await db.Events.AddAsync(academicEvent, ct);
}

internal sealed class EventReadModel(AppDbContext db) : IEventReadModel
{
    public async Task<IReadOnlyList<EventDto>> ListAsync(CancellationToken ct) =>
        await db.Events.AsNoTracking().OrderBy(e => e.Date)
            .Select(e => new EventDto(e.Id, e.Title, e.Category, e.Organizer, e.Date, e.Capacity, e.Status.ToString()))
            .ToListAsync(ct);
}
