using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Eventos;

public enum EstadoEvento { EnRevision, Aprobado, AprobadoConCambios, Rechazado }

public sealed record EventoCreado(Guid EventoId, DateTime OcurridoEn) : IDomainEvent;
public sealed record EventoAprobado(Guid EventoId, DateTime OcurridoEn) : IDomainEvent;

/// <summary>Agregado "Evento académico". Las reglas de negocio viven aquí, no en los handlers.</summary>
public sealed class Evento : AggregateRoot
{
    public string Titulo { get; private set; } = default!;
    public string Categoria { get; private set; } = default!;
    public string Responsable { get; private set; } = default!;
    public DateOnly Fecha { get; private set; }
    public int Cupos { get; private set; }
    public EstadoEvento Estado { get; private set; }

    private Evento() { } // EF Core

    public static Evento Crear(string titulo, string categoria, string responsable, DateOnly fecha, int cupos)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new DomainException("El título es obligatorio.");
        if (cupos <= 0) throw new DomainException("Los cupos deben ser mayores a cero.");

        var evento = new Evento
        {
            Titulo = titulo.Trim(), Categoria = categoria, Responsable = responsable,
            Fecha = fecha, Cupos = cupos, Estado = EstadoEvento.EnRevision,
        };
        evento.Levantar(new EventoCreado(evento.Id, DateTime.UtcNow));
        return evento;
    }

    public void Aprobar()
    {
        if (Estado == EstadoEvento.Rechazado) throw new DomainException("Un evento rechazado no puede aprobarse.");
        Estado = EstadoEvento.Aprobado;
        Levantar(new EventoAprobado(Id, DateTime.UtcNow));
    }

    public void Rechazar() => Estado = EstadoEvento.Rechazado;
}
