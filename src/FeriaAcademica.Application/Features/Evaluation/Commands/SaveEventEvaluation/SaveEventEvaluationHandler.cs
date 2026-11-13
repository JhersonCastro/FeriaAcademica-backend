using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Evaluation;
using FeriaAcademica.Domain.Events;
using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Commands.SaveEventEvaluation;

internal sealed class SaveEventEvaluationHandler(IAcademicEventRepository events, IRoleAssignmentRepository roles, IEvaluationRepository evaluations, IUnitOfWork uow)
    : ICommandHandler<SaveEventEvaluationCommand, Unit>
{
    public async ValueTask<Unit> Handle(SaveEventEvaluationCommand c, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(c.EventId, ct) ?? throw new DomainException("Event not found.");
        EvaluationPolicy.EnsureCanManage(c.ActorIsSuperAdmin, await roles.GetRolesAsync(c.ActorId, c.EventId, ct));

        var instrument = await evaluations.GetByEventAsync(c.EventId, ct);
        if (instrument is null)
        {
            if (c.Questions.Count == 0) return Unit.Value;   // nothing defined and nothing to define
            instrument = Instrument.Create(c.EventId, InstrumentType.Survey, EvaluatorGroup.Attendees, $"Evaluation of {academicEvent.Name}");
            await evaluations.AddAsync(instrument, ct);
        }

        var hasAnswers = await evaluations.HasResponsesAsync(instrument.Id, ct);
        evaluations.RemoveCriteria(instrument.ReplaceQuestions(c.Questions, hasAnswers, DateTime.UtcNow));
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
