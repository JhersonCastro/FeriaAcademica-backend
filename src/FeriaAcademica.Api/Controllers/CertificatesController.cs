using FeriaAcademica.Application.Features.Certificates.Commands.DownloadCertificate;
using FeriaAcademica.Application.Features.Certificates.Queries.ListMyCertificates;
using FeriaAcademica.Domain.Registrations;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

/// <summary>"My certificates": what the signed-in person qualifies for and the PDF download.</summary>
[ApiController, Route("api/certificates"), Authorize]
public class CertificatesController(IMediator mediator) : ControllerBase
{
    /// <summary>Attendance certificates (attended the event) and participation certificates (delivered a final version).</summary>
    [HttpGet("mine")]
    public async Task<IReadOnlyList<MyCertificateDto>> Mine(CancellationToken ct) => await mediator.Send(new ListMyCertificatesQuery(User.UserId()), ct);

    /// <summary>The certificate as a PDF; only once the event has finished and the person qualifies. <c>type</c> is Attendance or Participation.</summary>
    [HttpGet("events/{eventId:guid}/{type}/pdf")]
    public async Task<IActionResult> Download(Guid eventId, CertificateType type, CancellationToken ct)
    {
        var pdf = await mediator.Send(new DownloadCertificateCommand(eventId, User.UserId(), type), ct);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }
}
