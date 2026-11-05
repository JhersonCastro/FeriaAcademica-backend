using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Privacy;

/// <summary>A version of the personal-data policy that users are asked to accept.</summary>
public sealed class DataPolicy : Entity
{
    public string Version { get; private set; } = default!;
    public string Text { get; private set; } = default!;
    public DateTime EffectiveFrom { get; private set; }

    private DataPolicy() { } // EF Core

    public static DataPolicy Create(string version, string text, DateTime effectiveFrom)
    {
        if (string.IsNullOrWhiteSpace(version)) throw new DomainException("Version is required.");
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Text is required.");
        return new DataPolicy { Version = version.Trim(), Text = text, EffectiveFrom = effectiveFrom };
    }
}

/// <summary>A user's answer to one version of the data policy.</summary>
public sealed class Consent : Entity
{
    public Guid UserId { get; private set; }
    public Guid PolicyId { get; private set; }
    public bool Accepted { get; private set; }
    public DateTime At { get; private set; }

    private Consent() { } // EF Core

    public static Consent Create(Guid userId, Guid policyId, bool accepted, DateTime now) =>
        new() { UserId = userId, PolicyId = policyId, Accepted = accepted, At = now };
}
