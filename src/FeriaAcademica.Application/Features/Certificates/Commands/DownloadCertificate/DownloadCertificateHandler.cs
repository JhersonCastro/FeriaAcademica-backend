using System.Security.Cryptography;
using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Certificates.Commands.DownloadCertificate;

internal sealed class DownloadCertificateHandler(
    IAcademicEventRepository events, IRegistrationRepository registrations, IAttendanceRepository attendances, IWorkRepository works,
    ICertificateRepository certificates, IUserAccounts users, IPdfRenderer pdf, IUnitOfWork uow)
    : ICommandHandler<DownloadCertificateCommand, PdfFile>
{
    public async ValueTask<PdfFile> Handle(DownloadCertificateCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");

        var registration = await registrations.GetAsync(c.EventId, c.UserId, ct);
        var attended = registration is not null && await attendances.GetByRegistrationAsync(registration.Id, ct) is not null;
        var work = c.Type == CertificateType.Participation ? await works.GetWithFinalVersionAsync(c.EventId, c.UserId, ct) : null;
        CertificateEligibility.EnsureCanDownload(c.Type, academicEvent, attended, work);

        var certificate = await certificates.GetAsync(c.EventId, c.UserId, c.Type, ct);
        if (certificate is null)
        {
            certificate = Certificate.Create(c.EventId, c.UserId, c.Type, work?.Id, NewCode(), DateTime.UtcNow);
            await certificates.AddAsync(certificate, ct);
            await uow.SaveChangesAsync(ct);
        }

        var holder = await users.FindAsync(c.UserId, ct) ?? throw new DomainException("User not found.");
        var content = pdf.RenderCertificate(new CertificateDocument(holder.FullName, academicEvent.Name, c.Type, work?.Title, certificate.Code, certificate.IssuedAt));
        return new PdfFile($"certificado-{FileNames.Slug(academicEvent.Name)}-{certificate.Code}.pdf", content);
    }

    private static string NewCode() => "FA-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
}
