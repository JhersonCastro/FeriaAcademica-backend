using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Registrations;
using Mediator;

namespace FeriaAcademica.Application.Features.Certificates.Commands.DownloadCertificate;

/// <summary>Issues the certificate the first time it is asked for (it is a command because of that) and always returns the same one afterwards.</summary>
public sealed record DownloadCertificateCommand(Guid EventId, Guid UserId, CertificateType Type) : ICommand<PdfFile>;
