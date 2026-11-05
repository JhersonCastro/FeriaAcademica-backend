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

    public static Institution Create(string name, string? acronym = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Institution name is required.");
        return new Institution { Name = name.Trim(), Acronym = acronym };
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
