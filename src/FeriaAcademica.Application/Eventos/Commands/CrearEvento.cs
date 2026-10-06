using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Eventos;

namespace FeriaAcademica.Application.Eventos.Commands;

public sealed record CrearEventoCommand(string Titulo, string Categoria, string Responsable, DateOnly Fecha, int Cupos) : ICommand<Guid>;

internal sealed class CrearEventoHandler(IEventoRepository eventos, IUnitOfWork uow) : ICommandHandler<CrearEventoCommand, Guid>
{
    public async Task<Guid> Handle(CrearEventoCommand c, CancellationToken ct)
    {
        var evento = Evento.Crear(c.Titulo, c.Categoria, c.Responsable, c.Fecha, c.Cupos);
        await eventos.AgregarAsync(evento, ct);
        await uow.SaveChangesAsync(ct);
        return evento.Id;
    }
}
