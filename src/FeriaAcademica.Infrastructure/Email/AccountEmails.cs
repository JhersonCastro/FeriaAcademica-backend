using System.Net;
using System.Net.Mail;
using FeriaAcademica.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FeriaAcademica.Infrastructure.Email;

/// <summary>
/// Sends account emails through SMTP (section "Smtp" in user-secrets). Without an SMTP host it only logs:
/// in Development the log includes the link so the flow can be tested; elsewhere it does not, because the link grants access.
/// </summary>
internal sealed class AccountEmails(IConfiguration config, IHostEnvironment env, ILogger<AccountEmails> logger) : IAccountEmails
{
    public Task SendSetPasswordAsync(string email, string fullName, Guid userId, string token, CancellationToken ct) =>
        SendAsync(email, "Activa tu cuenta de FeriaAcademica",
            $"Hola {fullName}, se creó una cuenta para ti. Define tu contraseña en este enlace (vence en 24 horas):\n\n{PasswordLink("set-password", userId, token)}\n", ct);

    public Task SendPasswordResetAsync(string email, string fullName, Guid userId, string token, CancellationToken ct) =>
        SendAsync(email, "Recupera tu contraseña de FeriaAcademica",
            $"Hola {fullName}, define una nueva contraseña en este enlace (vence en 24 horas):\n\n{PasswordLink("reset-password", userId, token)}\n\nSi no lo pediste, ignora este correo.", ct);

    // Each link goes on its own line: punctuation right after a URL ends up inside it in some mail clients.
    public Task SendInvitationAsync(string email, string eventName, string role, string token, CancellationToken ct) =>
        SendAsync(email, $"Invitación a {eventName}",
            $"Te invitaron a {eventName} como {RoleName(role)}. Acepta la invitación en este enlace (vence en 7 días):\n\n{FrontendUrl}/invitations/{token}\n", ct);

    private static string RoleName(string role) => role switch
    {
        "Participant" => "estudiante participante",
        "Attendee" => "invitado",
        "Teacher" => "profesor",
        "Jury" => "jurado",
        "Coordinator" => "coordinador",
        _ => role,
    };

    private string FrontendUrl => (config["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');

    private string PasswordLink(string page, Guid userId, string token) =>
        $"{FrontendUrl}/account/{page}?userId={userId}&token={Uri.EscapeDataString(token)}";

    private async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        var smtp = config.GetSection("Smtp");
        var host = smtp["Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            if (env.IsDevelopment()) logger.LogWarning("SMTP is not configured. Email to {To}: {Subject}\n{Body}", to, subject, body);
            else logger.LogError("SMTP is not configured: the email \"{Subject}\" to {To} was not sent.", subject, to);
            return;
        }

        using var message = new MailMessage(smtp["From"] ?? smtp["User"] ?? throw new InvalidOperationException("Smtp:From is required."), to, subject, body);
        using var client = new SmtpClient(host, smtp.GetValue("Port", 587)) { EnableSsl = smtp.GetValue("EnableSsl", true) };
        if (!string.IsNullOrEmpty(smtp["User"])) client.Credentials = new NetworkCredential(smtp["User"], smtp["Password"]);
        await client.SendMailAsync(message, ct);
    }
}
