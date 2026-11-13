using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Evaluation;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Application.Features.Evaluation;

/// <summary>Loads what is needed to answer the evaluation of a work and checks the person may.</summary>
internal static class EvaluationAccess
{
    public static async Task<Instrument> EnsureCanAnswerAsync(
        Guid eventId, Guid workId, Guid userId, IAcademicEventRepository events, IWorkRepository works, IParticipationRepository participations,
        IRegistrationRepository registrations, IAttendanceRepository attendances, IEvaluationRepository evaluations, CancellationToken ct)
    {
        var academicEvent = await events.GetAsync(eventId, ct) ?? throw new DomainException("Event not found.");
        var work = await works.GetAsync(workId, ct) ?? throw new DomainException("Work not found.");
        var author = await participations.GetAsync(work.ParticipationId, ct);
        if (author is null || author.EventId != eventId) throw new DomainException("Work not found.");

        var registration = await registrations.GetAsync(eventId, userId, ct);
        var attended = registration is not null && await attendances.GetByRegistrationAsync(registration.Id, ct) is not null;
        EvaluationPolicy.EnsureCanAnswer(academicEvent, attended, work, author.UserId == userId);

        var instrument = await evaluations.GetByEventAsync(eventId, ct);
        return instrument is { Criteria.Count: > 0 } ? instrument : throw new DomainException("The event has no evaluation.");
    }
}
