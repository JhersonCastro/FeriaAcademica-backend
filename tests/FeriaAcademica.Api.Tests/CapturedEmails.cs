using FeriaAcademica.Application.Abstractions;

namespace FeriaAcademica.Api.Tests;

public sealed record SentLink(string Kind, string Email, Guid UserId, string Token);

/// <summary>Stands in for the SMTP sender: keeps the account links so a test can follow them.</summary>
public sealed class CapturedEmails : IAccountEmails
{
    private readonly List<SentLink> _sent = [];

    public IReadOnlyList<SentLink> Sent { get { lock (_sent) return [.. _sent]; } }

    public Task SendSetPasswordAsync(string email, string fullName, Guid userId, string token, CancellationToken ct) => Add("SetPassword", email, userId, token);

    public Task SendPasswordResetAsync(string email, string fullName, Guid userId, string token, CancellationToken ct) => Add("PasswordReset", email, userId, token);

    public Task SendInvitationAsync(string email, string eventName, string role, string token, CancellationToken ct) => Add("Invitation", email, Guid.Empty, token);

    private Task Add(string kind, string email, Guid userId, string token)
    {
        lock (_sent) _sent.Add(new SentLink(kind, email, userId, token));
        return Task.CompletedTask;
    }
}
