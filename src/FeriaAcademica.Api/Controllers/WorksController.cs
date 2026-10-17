using System.Security.Claims;
using FeriaAcademica.Application.Features.Works.Commands.AddArtifact;
using FeriaAcademica.Application.Features.Works.Commands.CreateWork;
using FeriaAcademica.Application.Features.Works.Commands.SubmitWork;
using FeriaAcademica.Application.Features.Works.Queries.GetWork;
using FeriaAcademica.Application.Features.Works.Queries.ListMyWorks;
using FeriaAcademica.Domain.Works;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

/// <summary>The author's side of a work: create it, attach artifacts, submit it before the deadline and follow its review.</summary>
[ApiController, Route("api/works"), Authorize]
public class WorksController(IMediator mediator) : ControllerBase
{
    public sealed record CreateWorkRequest(Guid ParticipationId, Guid ProfessorId, string Title, string? Summary);
    /// <summary>Send a <c>LinkUrl</c>, or a <c>FileUrl</c> with the original <c>FileName</c> and <c>FileSizeBytes</c>.</summary>
    public sealed record AddArtifactRequest(ArtifactType Type, string? LinkUrl, string? FileUrl, string? FileName, long? FileSizeBytes);

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>The signed-in participant's works, with their status and the feedback of the latest review.</summary>
    [HttpGet("mine")]
    public async Task<IReadOnlyList<MyWorkDto>> ListMine(CancellationToken ct) => await mediator.Send(new ListMyWorksQuery(UserId), ct);

    /// <summary>A work with its artifacts and all the reviewer feedback; only its author can read it.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        await mediator.Send(new GetWorkQuery(id, UserId), ct) is { } work ? Ok(work) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkRequest r, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateWorkCommand(r.ParticipationId, r.ProfessorId, r.Title, r.Summary, UserId), ct);
        return Created($"/api/works/{id}", new { id });
    }

    [HttpPost("{id:guid}/artifacts")]
    public async Task<IActionResult> AddArtifact(Guid id, AddArtifactRequest r, CancellationToken ct)
    {
        var artifact = await mediator.Send(new AddArtifactCommand(id, UserId, r.Type, r.LinkUrl, r.FileUrl, r.FileName, r.FileSizeBytes), ct);
        return Created($"/api/works/{id}", artifact);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        await mediator.Send(new SubmitWorkCommand(id, UserId), ct);
        return NoContent();
    }
}
