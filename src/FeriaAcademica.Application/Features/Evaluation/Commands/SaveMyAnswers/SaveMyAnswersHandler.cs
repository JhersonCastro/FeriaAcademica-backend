using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Evaluation;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Commands.SaveMyAnswers;

internal sealed class SaveMyAnswersHandler(
    IAcademicEventRepository events, IWorkRepository works, IParticipationRepository participations, IRegistrationRepository registrations,
    IAttendanceRepository attendances, IEvaluationRepository evaluations, IUnitOfWork uow) : ICommandHandler<SaveMyAnswersCommand, Unit>
{
    public async ValueTask<Unit> Handle(SaveMyAnswersCommand c, CancellationToken ct)
    {
        var instrument = await EvaluationAccess.EnsureCanAnswerAsync(c.EventId, c.WorkId, c.UserId, events, works, participations, registrations, attendances, evaluations, ct);
        var responses = instrument.BuildResponses(c.WorkId, c.UserId, c.Answers, DateTime.UtcNow);

        evaluations.RemoveResponses(await evaluations.GetResponsesAsync(instrument.Id, c.WorkId, c.UserId, ct));
        await evaluations.AddResponsesAsync(responses, ct);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
