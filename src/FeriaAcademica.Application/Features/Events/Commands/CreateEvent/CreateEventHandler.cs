using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Events.Commands.CreateEvent;

internal sealed class CreateEventHandler(IAcademicEventRepository events, IUnitOfWork uow) : ICommandHandler<CreateEventCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateEventCommand c, CancellationToken ct)
    {
        var stages = (c.Stages ?? []).Select(s => (Type: ParseStageType(s.Type), s.StartDate, s.EndDate)).ToList();
        if (stages.GroupBy(s => s.Type).FirstOrDefault(g => g.Count() > 1) is { } repeated)
            throw new DomainException($"The {repeated.Key} stage appears more than once.");

        var academicEvent = AcademicEvent.Create(c.InstitutionId, c.Name, c.CreatedBy, capacity: c.Capacity);
        foreach (var stage in stages) academicEvent.ScheduleStage(stage.Type, stage.StartDate.ToUniversalTime(), stage.EndDate.ToUniversalTime());
        await events.AddAsync(academicEvent, ct);
        await uow.SaveChangesAsync(ct);
        return academicEvent.Id;
    }

    private static StageType ParseStageType(string? value) =>
        Enum.TryParse<StageType>(value, ignoreCase: true, out var type) && Enum.IsDefined(type) && !int.TryParse(value, out _)
            ? type
            : throw new DomainException($"Unknown stage type '{value}'.");
}
