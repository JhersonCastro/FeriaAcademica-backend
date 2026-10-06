using FeriaAcademica.Application.Abstractions;

namespace FeriaAcademica.Application.Eventos.Queries;

public sealed record EventoDto(Guid Id, string Titulo, string Categoria, string Responsable, DateOnly Fecha, int Cupos, string Estado);

public sealed record ListarEventosQuery : IQuery<IReadOnlyList<EventoDto>>;

/// <summary>Lado "lectura" del CQRS: puerto propio para consultar (puede apuntar luego a otra base o a vistas).</summary>
public interface IEventoReadModel { Task<IReadOnlyList<EventoDto>> ListarAsync(CancellationToken ct); }

internal sealed class ListarEventosHandler(IEventoReadModel lectura) : IQueryHandler<ListarEventosQuery, IReadOnlyList<EventoDto>>
{
    public Task<IReadOnlyList<EventoDto>> Handle(ListarEventosQuery q, CancellationToken ct) => lectura.ListarAsync(ct);
}
