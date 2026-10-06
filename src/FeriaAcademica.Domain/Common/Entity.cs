namespace FeriaAcademica.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}

public interface IDomainEvent { DateTime OcurridoEn { get; } }

/// <summary>Raíz de agregado: único punto de entrada para modificar el agregado y levantar eventos de dominio.</summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _eventos = [];
    public IReadOnlyCollection<IDomainEvent> EventosDeDominio => _eventos;

    protected void Levantar(IDomainEvent evento) => _eventos.Add(evento);
    public void LimpiarEventos() => _eventos.Clear();
}

public class DomainException(string mensaje) : Exception(mensaje);
