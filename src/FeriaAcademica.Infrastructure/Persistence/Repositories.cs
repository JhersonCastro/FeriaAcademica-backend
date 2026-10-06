using FeriaAcademica.Application.Eventos.Queries;
using FeriaAcademica.Domain.Eventos;
using Microsoft.EntityFrameworkCore;

namespace FeriaAcademica.Infrastructure.Persistence;

/// <summary>Lado escritura: carga y guarda agregados completos.</summary>
internal sealed class EventoRepository(AppDbContext db) : IEventoRepository
{
    public Task<Evento?> ObtenerAsync(Guid id, CancellationToken ct) => db.Eventos.FirstOrDefaultAsync(e => e.Id == id, ct);
    public async Task AgregarAsync(Evento evento, CancellationToken ct) => await db.Eventos.AddAsync(evento, ct);
}

/// <summary>Lado lectura: proyecta directo a DTOs, sin cargar agregados.</summary>
internal sealed class EventoReadModel(AppDbContext db) : IEventoReadModel
{
    public async Task<IReadOnlyList<EventoDto>> ListarAsync(CancellationToken ct) =>
        await db.Eventos.AsNoTracking().OrderBy(e => e.Fecha)
            .Select(e => new EventoDto(e.Id, e.Titulo, e.Categoria, e.Responsable, e.Fecha, e.Cupos, e.Estado.ToString()))
            .ToListAsync(ct);
}
