namespace FeriaAcademica.Application.Abstractions;

/// <summary>Emails that carry an account link. The implementation builds the frontend URL and decides how the message is delivered.</summary>
public interface IAccountEmails
{
    Task SendSetPasswordAsync(string email, string fullName, Guid userId, string token, CancellationToken ct);
    Task SendPasswordResetAsync(string email, string fullName, Guid userId, string token, CancellationToken ct);
    Task SendInvitationAsync(string email, string eventName, string role, string token, CancellationToken ct);
}
