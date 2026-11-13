using FeriaAcademica.Domain.Evaluation;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Queries.GetMyWorkAnswers;

internal sealed class GetMyWorkAnswersHandler(
    IAcademicEventRepository events, IWorkRepository works, IParticipationRepository participations, IRegistrationRepository registrations,
    IAttendanceRepository attendances, IEvaluationRepository evaluations, IEvaluationReadModel readModel) : IQueryHandler<GetMyWorkAnswersQuery, MyWorkAnswersDto?>
{
    public async ValueTask<MyWorkAnswersDto?> Handle(GetMyWorkAnswersQuery q, CancellationToken ct)
    {
        await EvaluationAccess.EnsureCanAnswerAsync(q.EventId, q.WorkId, q.UserId, events, works, participations, registrations, attendances, evaluations, ct);
        return await readModel.GetMyAnswersAsync(q.EventId, q.WorkId, q.UserId, ct);
    }
}
