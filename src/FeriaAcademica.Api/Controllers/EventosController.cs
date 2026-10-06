using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Eventos.Commands;
using FeriaAcademica.Application.Eventos.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/eventos"), Authorize]
public class EventosController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record CrearEventoRequest(string Titulo, string Categoria, string Responsable, DateOnly Fecha, int Cupos);

    [HttpGet]
    public async Task<IReadOnlyList<EventoDto>> Listar(CancellationToken ct) => await dispatcher.Ask(new ListarEventosQuery(), ct);

    [HttpPost]
    public async Task<IActionResult> Crear(CrearEventoRequest r, CancellationToken ct)
    {
        var id = await dispatcher.Send(new CrearEventoCommand(r.Titulo, r.Categoria, r.Responsable, r.Fecha, r.Cupos), ct);
        return Created($"/api/eventos/{id}", new { id });
    }

    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Aprobar(Guid id, CancellationToken ct)
    {
        await dispatcher.Send(new AprobarEventoCommand(id), ct);
        return NoContent();
    }
}
