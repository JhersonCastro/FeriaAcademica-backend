using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Events;

public sealed class EventTemplate : Entity
{
    public Guid? InstitutionId { get; private set; }   // null = global template
    public string Name { get; private set; } = default!;
    public string ConfigJson { get; private set; } = default!;   // stages, roles, instruments, parameters, tie-break
    public bool IsActive { get; private set; } = true;
    public Guid CreatedBy { get; private set; }

    private EventTemplate() { } // EF Core

    public static EventTemplate Create(string name, string configJson, Guid createdBy, Guid? institutionId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(configJson)) throw new DomainException("Configuration is required.");
        return new EventTemplate { InstitutionId = institutionId, Name = name.Trim(), ConfigJson = configJson, CreatedBy = createdBy };
    }
}
