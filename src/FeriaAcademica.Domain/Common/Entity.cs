namespace FeriaAcademica.Domain.Common;

/// <summary>Base of every domain entity: identified by a Guid that the domain generates when the entity is created.</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}

/// <summary>A fact that happened in the domain.</summary>
public interface IDomainEvent { DateTime OccurredAt { get; } }

/// <summary>An entity that collects the <see cref="IDomainEvent"/>s it raises.</summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>A business rule was broken; the API answers it with a 400.</summary>
public class DomainException(string message) : Exception(message);
