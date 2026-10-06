using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Application.Events.Commands;

public sealed record ApproveEventCommand(Guid EventId) : ICommand<bool>;

internal sealed class ApproveEventHandler(IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<ApproveEventCommand, bool>
{
    public async Task<bool> Handle(ApproveEventCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        academicEvent.Approve();
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
