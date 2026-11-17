using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Institutions;

public enum MembershipPosition { Student, Teacher, Administrative }

/// <summary>An organization that hosts events, such as a university.</summary>
public sealed class Institution : Entity
{
    public string Name { get; private set; } = default!;
    public string? Acronym { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Institution() { } // EF Core

    public const int NameMaxLength = 200;
    public const int AcronymMaxLength = 30;

    public static Institution Create(string name, string? acronym = null)
    {
        var institution = new Institution();
        institution.Apply(name, acronym);
        return institution;
    }

    public void Update(string name, string? acronym) => Apply(name, acronym);

    /// <summary>An inactive institution cannot receive new events; its existing events stay as they are.</summary>
    public void Deactivate()
    {
        if (!IsActive) throw new DomainException("The institution is already inactive.");
        IsActive = false;
    }

    public void Activate()
    {
        if (IsActive) throw new DomainException("The institution is already active.");
        IsActive = true;
    }

    private void Apply(string name, string? acronym)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Institution name is required.");
        name = name.Trim();
        if (name.Length > NameMaxLength) throw new DomainException($"Institution name cannot exceed {NameMaxLength} characters.");
        acronym = string.IsNullOrWhiteSpace(acronym) ? null : acronym.Trim();
        if (acronym is { Length: > AcronymMaxLength }) throw new DomainException($"Institution acronym cannot exceed {AcronymMaxLength} characters.");
        Name = name;
        Acronym = acronym;
    }
}

/// <summary>A user's position in an institution, and whether they may create events there.</summary>
public sealed class InstitutionMembership : Entity
{
    public Guid UserId { get; private set; }
    public Guid InstitutionId { get; private set; }
    public MembershipPosition Position { get; private set; }
    public bool CanCreateEvents { get; private set; }
    public bool IsActive { get; private set; } = true;

    private InstitutionMembership() { } // EF Core

    public static InstitutionMembership Create(Guid userId, Guid institutionId, MembershipPosition position, bool canCreateEvents = false) =>
        new() { UserId = userId, InstitutionId = institutionId, Position = position, CanCreateEvents = canCreateEvents };
}
