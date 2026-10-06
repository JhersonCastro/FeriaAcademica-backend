namespace FeriaAcademica.Domain.Eventos;

public interface IEventoRepository
{
    Task<Evento?> ObtenerAsync(Guid id, CancellationToken ct);
    Task AgregarAsync(Evento evento, CancellationToken ct);
}
