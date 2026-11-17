using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Institutions;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.CreateEvent;

internal sealed class CreateEventHandler(IAcademicEventRepository events, IInstitutionRepository institutions, IUnitOfWork uow) : ICommandHandler<CreateEventCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateEventCommand c, CancellationToken ct)
    {
        var institution = await institutions.GetAsync(c.InstitutionId, ct) ?? throw new DomainException("The institution does not exist.");
        if (!institution.IsActive) throw new DomainException("The institution is inactive and cannot receive new events.");
        var academicEvent = AcademicEvent.Create(c.InstitutionId, c.Name, c.CreatedBy, capacity: c.Capacity);
        await events.AddAsync(academicEvent, ct);
        await uow.SaveChangesAsync(ct);
        return academicEvent.Id;
    }
}
