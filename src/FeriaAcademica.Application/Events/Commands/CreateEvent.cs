using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Application.Events.Commands;

public sealed record CreateEventCommand(string Title, string Category, string Organizer, DateOnly Date, int Capacity) : ICommand<Guid>;

internal sealed class CreateEventHandler(IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<CreateEventCommand, Guid>
{
    public async Task<Guid> Handle(CreateEventCommand c, CancellationToken ct)
    {
        var academicEvent = AcademicEvent.Create(c.Title, c.Category, c.Organizer, c.Date, c.Capacity);
        await events.AddAsync(academicEvent, ct);
        await uow.SaveChangesAsync(ct);
        return academicEvent.Id;
    }
}
