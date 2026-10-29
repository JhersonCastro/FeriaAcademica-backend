using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.CreateEvent;

internal sealed class CreateEventHandler(IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<CreateEventCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateEventCommand c, CancellationToken ct)
    {
        var academicEvent = AcademicEvent.Create(c.InstitutionId, c.Name, c.CreatedBy, capacity: c.Capacity);
        await events.AddAsync(academicEvent, ct);
        await uow.SaveChangesAsync(ct);
        return academicEvent.Id;
    }
}
