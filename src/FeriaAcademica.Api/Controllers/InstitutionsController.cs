using FeriaAcademica.Application.Features.Institutions.Commands.ActivateInstitution;
using FeriaAcademica.Application.Features.Institutions.Commands.CreateInstitution;
using FeriaAcademica.Application.Features.Institutions.Commands.DeactivateInstitution;
using FeriaAcademica.Application.Features.Institutions.Commands.UpdateInstitution;
using FeriaAcademica.Application.Features.Institutions.Queries.ListInstitutions;
using FeriaAcademica.Infrastructure.Identity;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/institutions"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
public class InstitutionsController(IMediator mediator) : ControllerBase
{
    public sealed record InstitutionRequest(string Name, string? Acronym);

    /// <summary>Every institution, active or not, ordered by name.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<InstitutionDto>> List(CancellationToken ct) => await mediator.Send(new ListInstitutionsQuery(), ct);

    [HttpPost]
    public async Task<IActionResult> Create(InstitutionRequest r, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateInstitutionCommand(r.Name, r.Acronym), ct);
        return Created($"/api/institutions/{id}", new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, InstitutionRequest r, CancellationToken ct) =>
        await mediator.Send(new UpdateInstitutionCommand(id, r.Name, r.Acronym), ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct) =>
        await mediator.Send(new DeactivateInstitutionCommand(id), ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct) =>
        await mediator.Send(new ActivateInstitutionCommand(id), ct) ? NoContent() : NotFound();
}
