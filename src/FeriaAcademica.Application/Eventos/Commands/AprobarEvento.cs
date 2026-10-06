using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Eventos;

namespace FeriaAcademica.Application.Eventos.Commands;

public sealed record AprobarEventoCommand(Guid EventoId) : ICommand<bool>;

internal sealed class AprobarEventoHandler(IEventoRepository eventos, IUnitOfWork uow) : ICommandHandler<AprobarEventoCommand, bool>
{
    public async Task<bool> Handle(AprobarEventoCommand c, CancellationToken ct)
    {
        var evento = await eventos.ObtenerAsync(c.EventoId, ct) ?? throw new DomainException("El evento no existe.");
        evento.Aprobar();
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
